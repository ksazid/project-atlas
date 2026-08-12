import { useRef, useState } from 'react';
import { ActivityIndicator, Pressable, StyleSheet, Text, TextInput, View } from 'react-native';
import { router } from 'expo-router';
import { createBusiness } from '@/api/atlas-client';
import {
  createBusinessFromDiscovery,
  discoverBusiness,
  enrichBusinessPlace,
  searchBusinessLocations,
  type BusinessDiscovery,
  type BusinessLocationSearchResponse,
  type BusinessPlaceEnrichmentResponse,
} from '@/api/business-discovery';
import { loadSession, saveSession } from '@/auth/session';
import { BrandMark } from '@/components/BrandMark';
import { AtlasScreen } from '@/components/AtlasScreen';
import {
  buildCreateBusinessFromDiscoveryRequest,
  canConfirmDiscovery,
  createDiscoveryDraft,
  getDiscoveryFact,
  getMissingRequiredFields,
  type DiscoveryDraft,
} from '@/features/business-discovery/discovery-model';
import {
  applyLocationToDraft,
  displayMarket,
  type BusinessLocationCandidate,
} from '@/features/business-discovery/location-model';
import {
  buildAboutBusinessItems,
  buildConfirmedOperatingContext,
} from '@/features/business-discovery/place-enrichment-model';
import {
  canonicalBusinessUrlKey,
  canonicalizeBusinessUrlInput,
} from '@/features/business-discovery/url-policy';

const emptyDraft: DiscoveryDraft = {
  snapshotId: '',
  name: '',
  category: '',
  subcategory: '',
  country: '',
  timezone: '',
  currency: '',
  primaryLocation: '',
  operatingStatus: 'Open',
  description: '',
  website: '',
  phone: '',
  email: '',
  socialChannels: '',
  businessHours: '',
  language: 'English',
};
const GREEN = '#00754A';

type Stage = 'discover' | 'confirm' | 'details' | 'manual';

export default function CreateBusinessScreen() {
  const [stage, setStage] = useState<Stage>('discover');
  const [sourceUrls, setSourceUrls] = useState<string[]>(['']);
  const [discovery, setDiscovery] = useState<BusinessDiscovery | null>(null);
  const [form, setForm] = useState<DiscoveryDraft>(emptyDraft);
  const [busy, setBusy] = useState(false);
  const [locationBusy, setLocationBusy] = useState(false);
  const [locationSearch, setLocationSearch] = useState('');
  const [locationResult, setLocationResult] = useState<BusinessLocationSearchResponse | null>(null);
  const [locationError, setLocationError] = useState<string | null>(null);
  const [placeEnrichment, setPlaceEnrichment] = useState<BusinessPlaceEnrichmentResponse | null>(null);
  const [placeEnrichmentBusy, setPlaceEnrichmentBusy] = useState(false);
  const [placeEnrichmentError, setPlaceEnrichmentError] = useState<string | null>(null);
  const [placeEnrichmentConfirmed, setPlaceEnrichmentConfirmed] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const placeEnrichmentRequestId = useRef(0);

  const update = (key: keyof DiscoveryDraft, value: string) => setForm(current => ({ ...current, [key]: value }));
  const sourceErrors = validateSourceUrls(sourceUrls);
  const canDiscover = !busy
    && canonicalBusinessUrlKey(sourceUrls[0] ?? '') !== null
    && sourceUrls.every(value => !value.trim() || canonicalBusinessUrlKey(value) !== null)
    && sourceErrors.every(value => !value);

  function updateSourceUrl(index: number, rawValue: string) {
    const normalized = canonicalizeBusinessUrlInput(rawValue);
    const value = normalized.complete && !normalized.error ? normalized.value : rawValue;
    setSourceUrls(current => current.map((item, itemIndex) => itemIndex === index ? value : item));
    setError(null);
  }

  function addSourceUrl() {
    setSourceUrls(current => current.length < 3 ? [...current, ''] : current);
    setError(null);
  }

  function removeSourceUrl(index: number) {
    setSourceUrls(current => index === 0
      ? current.map((value, itemIndex) => itemIndex === 0 ? '' : value)
      : current.filter((_, itemIndex) => itemIndex !== index));
    setError(null);
  }

  function clearPlaceEnrichment() {
    placeEnrichmentRequestId.current += 1;
    setPlaceEnrichment(null);
    setPlaceEnrichmentConfirmed(false);
    setPlaceEnrichmentError(null);
    setPlaceEnrichmentBusy(false);
  }

  async function loadPlaceEnrichment(
    location: BusinessLocationCandidate,
    accessToken?: string,
    snapshotId?: string | null,
  ) {
    const activeSnapshotId = snapshotId ?? discovery?.snapshotId ?? null;
    if (!activeSnapshotId) {
      clearPlaceEnrichment();
      return;
    }

    const requestId = placeEnrichmentRequestId.current + 1;
    placeEnrichmentRequestId.current = requestId;
    setPlaceEnrichment(null);
    setPlaceEnrichmentConfirmed(false);
    setPlaceEnrichmentError(null);
    setPlaceEnrichmentBusy(true);
    try {
      const token = accessToken ?? (await loadSession())?.accessToken;
      if (!token) {
        router.replace('/sign-in');
        return;
      }
      const result = await enrichBusinessPlace(token, activeSnapshotId, location.providerRef);
      if (requestId !== placeEnrichmentRequestId.current) return;
      if (buildAboutBusinessItems(result).length > 0) setPlaceEnrichment(result);
    } catch {
      if (requestId !== placeEnrichmentRequestId.current) return;
      setPlaceEnrichmentError('Atlas could not add extra public details. You can still continue.');
    } finally {
      if (requestId === placeEnrichmentRequestId.current) setPlaceEnrichmentBusy(false);
    }
  }

  async function analyse() {
    if (!canDiscover) return;
    const additionalUrls = sourceUrls.slice(1).map(value => canonicalBusinessUrlKey(value) ?? '').filter(Boolean);
    setBusy(true);
    setError(null);
    setLocationError(null);
    clearPlaceEnrichment();
    try {
      const session = await loadSession();
      if (!session) {
        router.replace('/sign-in');
        return;
      }
      const result = await discoverBusiness(session.accessToken, sourceUrls[0], additionalUrls);
      const draft = createDiscoveryDraft(result);
      setDiscovery(result);
      setForm(draft);
      setLocationSearch(draft.name);
      setStage('confirm');
      await resolveLocations(session.accessToken, result.snapshotId, draft.name || undefined);
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : 'Atlas could not analyse those business pages.');
    } finally {
      setBusy(false);
    }
  }

  async function resolveLocations(accessToken?: string, snapshotId?: string | null, query?: string) {
    if (locationBusy) return;
    setLocationBusy(true);
    setLocationError(null);
    clearPlaceEnrichment();
    try {
      const token = accessToken ?? (await loadSession())?.accessToken;
      if (!token) {
        router.replace('/sign-in');
        return;
      }
      const activeSnapshotId = snapshotId ?? discovery?.snapshotId ?? null;
      const searchQuery = query?.trim() || locationSearch.trim() || form.name.trim();
      const result = await searchBusinessLocations(token, activeSnapshotId, searchQuery || undefined);
      setLocationResult(result);
      if (result.selected) {
        setForm(current => applyLocationToDraft(current, result.selected!));
        await loadPlaceEnrichment(result.selected, token, activeSnapshotId);
      } else {
        setForm(current => ({ ...current, primaryLocation: '', country: '', timezone: '', currency: '' }));
      }
    } catch (cause) {
      setLocationResult(current => current ?? { state: 'search', candidates: [], selected: null, canChange: true });
      setLocationError(cause instanceof Error ? cause.message : 'Atlas could not search business locations.');
    } finally {
      setLocationBusy(false);
    }
  }

  function selectLocation(location: BusinessLocationCandidate) {
    setForm(current => applyLocationToDraft(current, location));
    setLocationResult(current => ({
      state: 'preselected',
      candidates: current?.candidates ?? [location],
      selected: location,
      canChange: true,
    }));
    setLocationError(null);
    void loadPlaceEnrichment(location);
  }

  function changeLocation() {
    clearPlaceEnrichment();
    setForm(current => ({ ...current, primaryLocation: '', country: '', timezone: '', currency: '' }));
    setLocationResult(current => current && current.candidates.length > 1
      ? { ...current, state: 'choose', selected: null }
      : { state: 'search', candidates: current?.candidates ?? [], selected: null, canChange: true });
    setLocationSearch(form.name);
  }

  async function submit() {
    if (busy || !canConfirmDiscovery(form)) return;
    setBusy(true);
    setError(null);
    try {
      const session = await loadSession();
      if (!session) {
        router.replace('/sign-in');
        return;
      }

      const confirmedOperatingContext = placeEnrichmentConfirmed && placeEnrichment
        ? buildConfirmedOperatingContext(placeEnrichment)
        : undefined;
      const business = discovery
        ? await createBusinessFromDiscovery(
            session.accessToken,
            buildCreateBusinessFromDiscoveryRequest(form, confirmedOperatingContext),
          )
        : await createBusiness(session.accessToken, {
            name: form.name.trim(),
            category: form.category.trim(),
            country: form.country.trim(),
            timezone: form.timezone.trim(),
            currency: form.currency.trim(),
            primaryLocation: form.primaryLocation.trim(),
            operatingStatus: form.operatingStatus.trim(),
          });

      await saveSession({ ...session, businessId: business.id });
      router.replace('/progressive-questions');
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : 'Atlas could not finish business setup.');
    } finally {
      setBusy(false);
    }
  }

  const missing = getMissingRequiredFields(form);

  function locationResolver() {
    const selected = locationResult?.selected;
    if (selected) return (
      <View style={s.locationPanel}>
        <View style={s.locationHeaderRow}>
          <View style={s.locationHeaderCopy}>
            <Text style={s.sectionTitle}>Your business location</Text>
            <Text style={s.locationHint}>Atlas uses this branch for local facts and recommendations.</Text>
          </View>
          <Text style={s.locationCheck}>✓</Text>
        </View>
        <Text style={s.locationName}>{selected.name}</Text>
        <Text style={s.locationAddress}>⌖  {selected.formattedAddress}</Text>
        <Text style={s.locationMarket}>{displayMarket(selected)}</Text>
        <Pressable accessibilityRole="button" accessibilityLabel="Change location" onPress={changeLocation} style={({ pressed }) => [s.locationLink, pressed && s.pressed]}>
          <Text style={s.editText}>Change location</Text>
        </Pressable>
      </View>
    );

    if (locationResult?.state === 'choose' && locationResult.candidates.length > 1) return (
      <View style={s.locationPanel}>
        <Text style={s.sectionTitle}>Which location are you setting up?</Text>
        <Text style={s.locationHint}>We found more than one possible branch. Choose the one you want Atlas to work with first.</Text>
        <View style={s.locationList}>
          {locationResult.candidates.map(candidate => (
            <Pressable
              key={candidate.providerRef}
              accessibilityRole="button"
              accessibilityLabel={`Use ${candidate.name}, ${candidate.formattedAddress}`}
              onPress={() => selectLocation(candidate)}
              style={({ pressed }) => [s.locationCard, pressed && s.pressed]}
            >
              <View style={s.locationDot} />
              <View style={s.locationCardCopy}>
                <Text style={s.locationName}>{candidate.name}</Text>
                <Text style={s.locationAddress}>{candidate.formattedAddress}</Text>
                <Text style={s.locationMarket}>{displayMarket(candidate)}</Text>
              </View>
              <Text style={s.chevron}>›</Text>
            </Pressable>
          ))}
        </View>
        <Pressable accessibilityRole="button" accessibilityLabel="Search another business location" onPress={() => { clearPlaceEnrichment(); setLocationResult({ state: 'search', candidates: [], selected: null, canChange: true }); }} style={({ pressed }) => [s.locationLink, pressed && s.pressed]}>
          <Text style={s.editText}>Search another location</Text>
        </Pressable>
      </View>
    );

    return (
      <View style={s.locationPanel}>
        <Text style={s.sectionTitle}>Find your business location</Text>
        <Text style={s.locationHint}>Search by business name, area or address. Atlas will set country, timezone and currency automatically.</Text>
        <View style={s.locationSearchRow}>
          <TextInput
            accessibilityLabel="Business location search"
            autoCapitalize="words"
            returnKeyType="search"
            onSubmitEditing={() => void resolveLocations(undefined, discovery?.snapshotId ?? null, locationSearch)}
            value={locationSearch}
            onChangeText={setLocationSearch}
            placeholder="Business name, area or address"
            placeholderTextColor="#7A8781"
            style={s.locationInput}
          />
          <Pressable
            accessibilityRole="button"
            accessibilityLabel="Search Google Maps"
            accessibilityState={{ busy: locationBusy, disabled: locationBusy || !locationSearch.trim() }}
            disabled={locationBusy || !locationSearch.trim()}
            onPress={() => void resolveLocations(undefined, discovery?.snapshotId ?? null, locationSearch)}
            style={({ pressed }) => [s.locationSearchButton, (locationBusy || !locationSearch.trim()) && s.disabled, pressed && s.pressed]}
          >
            {locationBusy ? <ActivityIndicator color="#FFF" size="small" /> : <Text style={s.locationSearchButtonText}>Search</Text>}
          </Pressable>
        </View>
        {locationError ? <Text accessibilityLiveRegion="polite" style={s.locationError}>{locationError}</Text> : null}
        {locationResult && locationResult.candidates.length === 0 && !locationBusy && !locationError ? <Text style={s.locationHint}>No reliable location found yet. Try the business name plus its town or area.</Text> : null}
      </View>
    );
  }

  if (stage === 'discover') return (
    <AtlasScreen contentStyle={s.container} keyboardShouldPersistTaps="handled" showsVerticalScrollIndicator={false}>
      <Back />
      <Text style={s.eyebrow}>AI ANALYSIS</Text>
      <Text style={s.title}>Discovering your{`\n`}business ✨</Text>
      <Text style={s.body}>Share your main public business page. Add up to two more public pages when they help Atlas confirm the same business.</Text>
      <View style={s.sourceList}>
        {sourceUrls.map((sourceUrl, index) => (
          <View key={`source-${index}`} style={s.sourceRow}>
            <View style={s.url}>
              <Text style={s.urlIcon}>{index === 0 ? '⊕' : '+'}</Text>
              <TextInput
                accessibilityLabel={index === 0 ? 'Business page URL' : `Additional business page URL ${index}`}
                autoCapitalize="none"
                autoCorrect={false}
                keyboardType="url"
                returnKeyType={index === 0 ? 'go' : 'done'}
                onSubmitEditing={index === 0 ? analyse : undefined}
                value={sourceUrl}
                onChangeText={value => updateSourceUrl(index, value)}
                placeholder={index === 0 ? 'https://yourbusiness.com' : 'Optional public business page'}
                placeholderTextColor="#6F7974"
                style={s.urlInput}
              />
              {busy && index === 0 ? (
                <ActivityIndicator color={GREEN} />
              ) : index === 0 && sourceUrl.length > 0 ? (
                <Pressable
                  accessibilityRole="button"
                  accessibilityLabel="Clear business page URL"
                  accessibilityHint="Clear primary business page URL"
                  hitSlop={8}
                  onPress={() => removeSourceUrl(0)}
                  style={({ pressed }) => [s.urlClear, pressed && s.pressed]}
                >
                  <Text style={s.urlClearText}>×</Text>
                </Pressable>
              ) : index > 0 ? (
                <Pressable
                  accessibilityRole="button"
                  accessibilityLabel="Remove additional business page URL"
                  hitSlop={8}
                  onPress={() => removeSourceUrl(index)}
                  style={({ pressed }) => [s.urlClear, pressed && s.pressed]}
                >
                  <Text style={s.urlClearText}>×</Text>
                </Pressable>
              ) : (
                <View style={s.spinner} />
              )}
            </View>
            {sourceErrors[index] ? <Text accessibilityLiveRegion="polite" style={s.sourceError}>{sourceErrors[index]}</Text> : null}
          </View>
        ))}
        {sourceUrls.length < 3 ? (
          <Pressable accessibilityRole="button" accessibilityLabel="Add another business page URL" onPress={addSourceUrl} style={({ pressed }) => [s.addSource, pressed && s.pressed]}>
            <Text style={s.addSourceText}>+ Add another business page URL</Text>
          </Pressable>
        ) : null}
      </View>
      <View style={s.orbitWrap}>
        <View style={s.orbitOuter} />
        <View style={s.orbitMid} /><View style={s.orbitInner} />
        <View style={s.bot}><View style={s.botCap} /><Text style={s.botFace}>●  ●{`\n`}⌣</Text></View>
        <Bubble icon="⌕" pos={s.b1} /><Bubble icon="♟" pos={s.b2} /><Bubble icon="▥" pos={s.b3} /><Bubble icon="▤" pos={s.b4} />
      </View>
      <Text style={s.analysisCopy}>Reading your public business pages and{`\n`}preparing facts for your review…</Text>
      <View style={s.checklist}>
        <Check text="Scanning public business pages" done={busy} />
        <Check text="Reading business information" done={busy} />
        <Check text="Detecting category" done={busy} />
        <Check text="Finding location details" done={busy} />
        <Check text="Preparing owner confirmation" />
      </View>
      {error ? <View style={s.errorBox}><Text accessibilityLiveRegion="polite" style={s.error}>{error}</Text></View> : null}
      {!busy ? <Pressable accessibilityLabel="Discover my business" accessibilityRole="button" accessibilityState={{ disabled: !canDiscover }} disabled={!canDiscover} onPress={analyse} style={({ pressed }) => [s.discoverButton, !canDiscover && s.disabled, pressed && s.pressed]}><Text style={s.discoverButtonText}>Discover my business</Text></Pressable> : null}
      {!busy ? <Pressable accessibilityLabel="Set up manually instead" accessibilityRole="button" onPress={() => { setDiscovery(null); setForm(emptyDraft); setLocationResult(null); setLocationSearch(''); clearPlaceEnrichment(); setError(null); setStage('manual'); }} style={({ pressed }) => [s.edit, pressed && s.pressed]}><Text style={s.editText}>Set up manually instead</Text></Pressable> : null}
    </AtlasScreen>
  );

  if (stage === 'confirm' && discovery) {
    const confidence = getDiscoveryFact(discovery, 'name')?.confidence ?? 'observed';
    const aboutItems = placeEnrichment ? buildAboutBusinessItems(placeEnrichment) : [];
    const thirdPartyAttributions = placeEnrichment?.attributions
      .map(attribution => attribution.provider.trim())
      .filter(Boolean) ?? [];
    return (
      <AtlasScreen contentStyle={s.container} keyboardShouldPersistTaps="handled" showsVerticalScrollIndicator={false}>
        <Back />
        <View style={s.confetti}><Text style={s.confettiText}>◆   ◇        ◆       ◇</Text></View>
        <View style={s.success}><Text style={s.successText}>✓</Text></View>
        <Text style={s.eyebrow}>CONFIRM</Text>
        <Text style={s.title}>We found your business!</Text>
        <Text style={s.body}>Review the public facts and choose the exact branch Atlas should work with. You stay in control.</Text>

        <View style={s.businessCard}>
          <BrandMark size={76} style={s.businessLogo} />
          <View style={s.businessCopy}>
            <View style={s.nameRow}>
              <Text numberOfLines={2} style={s.businessName}>{form.name || 'Business name needs confirmation'}</Text>
              <View style={s.verified}><Text style={s.verifiedText}>PUBLIC · {confidence.toUpperCase()}</Text></View>
            </View>
            {form.category ? <View style={s.categoryPill}><Text style={s.categoryPillText}>{humanize(form.category)}</Text></View> : null}
            <Text style={s.rating}>{discovery.facts.length} public facts ready for review</Text>
            <Text numberOfLines={1} style={s.site}>⊕  Public business page</Text>
          </View>
        </View>

        {locationResolver()}

        {placeEnrichmentBusy ? (
          <View style={s.aboutLoading}>
            <ActivityIndicator accessibilityLabel="Loading extra public business details" color={GREEN} size="small" />
            <Text style={s.aboutLoadingCopy}>Checking a few useful operating details…</Text>
          </View>
        ) : null}
        {placeEnrichmentError ? <Text accessibilityLiveRegion="polite" style={s.aboutError}>{placeEnrichmentError}</Text> : null}
        {placeEnrichment && aboutItems.length > 0 ? (
          <View style={s.aboutCard}>
            <View style={s.aboutHeader}>
              <Text style={s.sectionTitle}>About your business</Text>
              <Text style={s.aboutHint}>These public operating details are shown only for your review. Confirm them only if they are correct for this Business.</Text>
            </View>
            <View style={s.aboutList}>
              {aboutItems.map(item => (
                <View key={item.label} style={s.aboutItem}>
                  <Text style={s.aboutLabel}>{item.label}</Text>
                  <Text style={s.aboutValue}>{item.value}</Text>
                </View>
              ))}
            </View>
            <View style={s.divider} />
            <Text style={s.attribution}>Source: {placeEnrichment.attributionLabel || 'Google Maps'}</Text>
            {thirdPartyAttributions.length > 0 ? <Text style={s.attribution}>Additional attribution: {thirdPartyAttributions.join(' · ')}</Text> : null}
            <Pressable
              accessibilityRole="checkbox"
              accessibilityLabel="Confirm these operating details"
              accessibilityState={{ checked: placeEnrichmentConfirmed }}
              onPress={() => setPlaceEnrichmentConfirmed(current => !current)}
              style={({ pressed }) => [s.confirmationToggle, placeEnrichmentConfirmed && s.confirmationToggleSelected, pressed && s.pressed]}
            >
              <View style={[s.confirmationBox, placeEnrichmentConfirmed && s.confirmationBoxSelected]}>
                <Text style={s.confirmationCheck}>{placeEnrichmentConfirmed ? '✓' : ''}</Text>
              </View>
              <View style={s.confirmationCopy}>
                <Text style={s.confirmationTitle}>Confirm these operating details</Text>
                <Text style={s.confirmationHint}>Only details you explicitly confirm are added to Atlas as owner-provided business context.</Text>
              </View>
            </Pressable>
          </View>
        ) : null}

        <View style={s.detailsCard}>
          <Text style={s.sectionTitle}>Business details</Text>
          {form.website.trim() ? <Detail icon="⊕" text={form.website.trim()} /> : null}
          {form.phone.trim() ? <Detail icon="☎" text={form.phone.trim()} /> : null}
          {form.email?.trim() ? <Detail icon="@" text={form.email.trim()} /> : null}
          {form.socialChannels?.trim() ? <Detail icon="↗" text={form.socialChannels.trim()} /> : null}
          {form.businessHours.trim() ? <Detail icon="◷" text={form.businessHours.trim()} /> : null}
          {form.description.trim() ? <Detail icon="≡" text={form.description.trim()} /> : null}
          <Detail icon="⊕" text="Observed from public business page" />
          <View style={s.divider} />
          <Text style={s.sectionTitle}>Categories</Text>
          <View style={s.chipRow}>
            {form.category ? <Chip text={humanize(form.category)} /> : null}
            {form.subcategory ? <Chip text={humanize(form.subcategory)} /> : null}
          </View>
        </View>

        {missing.length > 0 ? <View style={s.missingBox}><Text style={s.missingTitle}>One thing still needs your review</Text><Text style={s.missingText}>{missing.map(humanizeField).join(' · ')}</Text></View> : null}
        {error ? <View style={s.errorBox}><Text accessibilityLiveRegion="polite" style={s.error}>{error}</Text></View> : null}
        <Pressable accessibilityLabel="Confirm and continue" accessibilityRole="button" accessibilityState={{ busy, disabled: busy || !canConfirmDiscovery(form) }} disabled={busy || !canConfirmDiscovery(form)} onPress={() => void submit()} style={({ pressed }) => [s.primary, (busy || !canConfirmDiscovery(form)) && s.disabled, pressed && s.pressed]}>
          {busy ? <ActivityIndicator color="#FFF" /> : <Text style={s.primaryText}>Confirm and continue</Text>}
        </Pressable>
        <Pressable accessibilityLabel="Edit details" accessibilityRole="button" onPress={() => setStage('details')} style={({ pressed }) => [s.edit, pressed && s.pressed]}><Text style={s.editText}>Edit details</Text></Pressable>
      </AtlasScreen>
    );
  }

  const isManual = stage === 'manual';
  return (
    <AtlasScreen contentStyle={s.container} keyboardShouldPersistTaps="handled" showsVerticalScrollIndicator={false}>
      <Back />
      <Text style={s.eyebrow}>{isManual ? 'MANUAL SETUP' : 'EDIT DETAILS'}</Text>
      <Text style={s.title}>{isManual ? 'Tell Atlas about your business.' : 'Correct anything that needs it.'}</Text>
      <Text style={s.body}>{isManual ? 'Add the basic business facts, then choose the operating location. Atlas handles country, timezone and currency for you.' : 'Public values are editable. Your selected location remains the source for country, timezone and currency.'}</Text>
      <Field label="Business name" value={form.name} onChange={value => { update('name', value); if (!locationSearch.trim()) setLocationSearch(value); }} />
      <Field label="Category" value={form.category} onChange={value => update('category', value)} hint="restaurant-cafe, retail, professional-services…" />
      <Field label="Subcategory (optional)" value={form.subcategory} onChange={value => update('subcategory', value)} />
      {locationResolver()}
      <Field label="Description (optional)" value={form.description} onChange={value => update('description', value)} multiline />
      <Field label="Website (optional)" value={form.website} onChange={value => update('website', value)} />
      <Field label="Phone (optional)" value={form.phone} onChange={value => update('phone', value)} />
      <Field label="Business email (optional)" value={form.email ?? ''} onChange={value => update('email', value)} />
      <Field label="Social channels (optional)" value={form.socialChannels ?? ''} onChange={value => update('socialChannels', value)} />
      <Field label="Opening hours (optional)" value={form.businessHours} onChange={value => update('businessHours', value)} />
      <Field label="Language" value={form.language} onChange={value => update('language', value)} />
      {error ? <View style={s.errorBox}><Text accessibilityLiveRegion="polite" style={s.error}>{error}</Text></View> : null}
      {discovery ? (
        <Pressable accessibilityLabel="Review details" accessibilityRole="button" accessibilityState={{ disabled: !canConfirmDiscovery(form) }} disabled={!canConfirmDiscovery(form)} onPress={() => setStage('confirm')} style={({ pressed }) => [s.primary, !canConfirmDiscovery(form) && s.disabled, pressed && s.pressed]}><Text style={s.primaryText}>Review details</Text></Pressable>
      ) : (
        <Pressable accessibilityLabel="Create business" accessibilityRole="button" accessibilityState={{ busy, disabled: busy || !canConfirmDiscovery(form) }} disabled={busy || !canConfirmDiscovery(form)} onPress={() => void submit()} style={({ pressed }) => [s.primary, (busy || !canConfirmDiscovery(form)) && s.disabled, pressed && s.pressed]}>{busy ? <ActivityIndicator color="#FFF" /> : <Text style={s.primaryText}>Create business</Text>}</Pressable>
      )}
    </AtlasScreen>
  );
}

function validateSourceUrls(values: string[]): (string | null)[] {
  const errors = values.map(value => {
    if (!value.trim()) return null;
    return canonicalizeBusinessUrlInput(value).error;
  });
  const seen = new Map<string, number>();
  values.forEach((value, index) => {
    const key = canonicalBusinessUrlKey(value);
    if (!key) return;
    const first = seen.get(key);
    if (first === undefined) seen.set(key, index);
    else {
      errors[first] = errors[first] ?? 'This business page is already added.';
      errors[index] = 'This business page is already added.';
    }
  });
  return errors;
}

function Back() {
  const goBack = () => {
    if (router.canGoBack()) router.back();
    else router.replace('/sign-in');
  };
  return <Pressable accessibilityRole="button" accessibilityLabel="Back" onPress={goBack} style={({ pressed }) => [s.back, pressed && s.pressed]}><Text style={s.backText}>←</Text></Pressable>;
}
function Bubble({ icon, pos }: { icon: string; pos: object }) { return <View style={[s.bubble, pos]}><Text style={s.bubbleText}>{icon}</Text></View>; }
function Check({ text, done = false }: { text: string; done?: boolean }) { return <View style={s.check}><Text style={s.checkIcon}>◎</Text><Text style={s.checkText}>{text}</Text><View style={[s.state, done && s.stateDone]}><Text style={s.stateText}>{done ? '✓' : ''}</Text></View></View>; }
function Detail({ icon, text }: { icon: string; text: string }) { return <View style={s.detail}><Text style={s.detailIcon}>{icon}</Text><Text style={s.detailText}>{text}</Text></View>; }
function Chip({ text }: { text: string }) { return <View style={s.chip}><Text style={s.chipText}>{text}</Text></View>; }
function Field({ label, value, onChange, hint, multiline = false }: { label: string; value: string; onChange: (value: string) => void; hint?: string; multiline?: boolean }) { return <View><Text style={s.fieldLabel}>{label}</Text><TextInput accessibilityLabel={label} multiline={multiline} placeholder={hint} placeholderTextColor="#7A8781" textAlignVertical={multiline ? 'top' : 'center'} value={value} onChangeText={onChange} style={[s.fieldInput, multiline && s.fieldInputMultiline]} /></View>; }
function humanize(value: string) { return value.split('-').filter(Boolean).map(part => part.charAt(0).toUpperCase() + part.slice(1)).join(' '); }
function humanizeField(value: string) { return value === 'location' ? 'Business location' : value.replace(/([a-z])([A-Z])/g, '$1 $2').replace(/^./, char => char.toUpperCase()); }

const s = StyleSheet.create({
  container: { gap: 15, backgroundColor: '#FFF' },
  back: { width: 40, height: 40, alignItems: 'center', justifyContent: 'center', marginLeft: -6 }, backText: { fontSize: 28, color: '#15231E' },
  eyebrow: { fontSize: 11, fontWeight: '900', letterSpacing: .7, color: GREEN }, title: { fontFamily: 'Georgia', fontSize: 33, lineHeight: 38, fontWeight: '800', letterSpacing: -.45, color: '#0A2F25' }, body: { fontSize: 13.5, lineHeight: 20.5, color: '#3E4D47', maxWidth: 330 },
  sourceList: { gap: 9 }, sourceRow: { gap: 5 }, sourceError: { fontSize: 11.2, lineHeight: 16, color: '#A1251B', paddingHorizontal: 2 }, addSource: { alignSelf: 'flex-start', minHeight: 44, justifyContent: 'center', paddingHorizontal: 2 }, addSourceText: { color: GREEN, fontSize: 12.5, fontWeight: '800' },
  url: { minHeight: 54, borderRadius: 10, borderWidth: 1, borderColor: '#E0E6E2', backgroundColor: '#FFF', flexDirection: 'row', alignItems: 'center', gap: 10, paddingHorizontal: 14, shadowColor: '#173B2A', shadowOpacity: .035, shadowRadius: 8, shadowOffset: { width: 0, height: 3 }, elevation: 1 }, urlIcon: { fontSize: 16, color: '#626F69' }, urlInput: { flex: 1, fontSize: 13, color: '#23322C' }, spinner: { width: 19, height: 19, borderRadius: 10, borderWidth: 2, borderColor: '#A5D9C0', borderRightColor: GREEN }, urlClear: { width: 24, height: 24, borderRadius: 12, alignItems: 'center', justifyContent: 'center', backgroundColor: '#F1F4F2' }, urlClearText: { fontSize: 18, lineHeight: 20, color: '#52615A', fontWeight: '700' },
  orbitWrap: { height: 245, alignItems: 'center', justifyContent: 'center' }, orbitOuter: { position: 'absolute', width: 214, height: 214, borderRadius: 107, borderWidth: 1.5, borderColor: '#67C49B' }, orbitMid: { position: 'absolute', width: 156, height: 156, borderRadius: 78, borderWidth: 1, borderColor: '#A9DCC6' }, orbitInner: { position: 'absolute', width: 104, height: 104, borderRadius: 52, backgroundColor: '#F1FAF5', borderWidth: 1, borderColor: '#D9EFE4' },
  bot: { width: 81, height: 70, borderRadius: 27, backgroundColor: '#0D3A30', alignItems: 'center', justifyContent: 'center', borderWidth: 8, borderColor: '#F4FBF7', shadowColor: '#1B5B44', shadowOpacity: .13, shadowRadius: 10, elevation: 3 }, botCap: { position: 'absolute', top: -12, width: 18, height: 12, borderTopLeftRadius: 8, borderTopRightRadius: 8, backgroundColor: '#8CCEB0' }, botFace: { fontSize: 13, lineHeight: 19, textAlign: 'center', color: '#36D78B', fontWeight: '900' },
  bubble: { position: 'absolute', width: 42, height: 42, borderRadius: 21, backgroundColor: '#FFF', alignItems: 'center', justifyContent: 'center', shadowColor: '#173B2A', shadowOpacity: .08, shadowRadius: 9, shadowOffset: { width: 0, height: 4 }, elevation: 2 }, bubbleText: { fontSize: 18, color: GREEN, fontWeight: '800' }, b1: { left: 15, top: 72 }, b2: { right: 15, top: 72 }, b3: { left: 28, bottom: 28 }, b4: { right: 28, bottom: 28 },
  analysisCopy: { textAlign: 'center', fontSize: 12.8, lineHeight: 18.5, color: '#1F2D28', fontWeight: '700' },
  checklist: { borderWidth: 1, borderColor: '#E5EAE7', borderRadius: 11, overflow: 'hidden', backgroundColor: '#FFF', shadowColor: '#173B2A', shadowOpacity: .025, shadowRadius: 6, elevation: 1 }, check: { minHeight: 44, backgroundColor: '#FFF', flexDirection: 'row', alignItems: 'center', paddingHorizontal: 13, gap: 11, borderBottomWidth: 1, borderBottomColor: '#EDF0EE' }, checkIcon: { fontSize: 14, color: '#35433E' }, checkText: { flex: 1, fontSize: 11.7, color: '#24322D' }, state: { width: 18, height: 18, borderRadius: 9, borderWidth: 1, borderColor: '#C8D2CD', alignItems: 'center', justifyContent: 'center' }, stateDone: { backgroundColor: GREEN, borderColor: GREEN }, stateText: { fontSize: 10, color: '#FFF', fontWeight: '900' },
  discoverButton: { minHeight: 50, borderRadius: 10, backgroundColor: '#008B58', alignItems: 'center', justifyContent: 'center' }, discoverButtonText: { color: '#FFF', fontSize: 14, fontWeight: '800' },
  confetti: { position: 'absolute', right: 20, top: 65 }, confettiText: { fontSize: 13, color: '#2FAF78' }, success: { alignSelf: 'center', width: 58, height: 58, borderRadius: 29, backgroundColor: GREEN, alignItems: 'center', justifyContent: 'center', marginBottom: 1 }, successText: { fontSize: 31, color: '#FFF', fontWeight: '700' },
  businessCard: { borderWidth: 1, borderColor: '#E4E9E6', borderRadius: 13, padding: 13, flexDirection: 'row', gap: 13, backgroundColor: '#FFF', shadowColor: '#173B2A', shadowOpacity: .035, shadowRadius: 7, elevation: 1 }, businessLogo: { width: 76, height: 76, resizeMode: 'contain' }, businessCopy: { flex: 1, gap: 5 }, nameRow: { flexDirection: 'row', alignItems: 'flex-start', gap: 7 }, businessName: { fontSize: 17, lineHeight: 22, fontWeight: '900', color: '#12221C', flex: 1 }, verified: { backgroundColor: '#E1F4E9', paddingHorizontal: 8, paddingVertical: 5, borderRadius: 10 }, verifiedText: { fontSize: 8.4, fontWeight: '800', color: GREEN }, categoryPill: { alignSelf: 'flex-start', backgroundColor: '#E5F5EB', paddingHorizontal: 10, paddingVertical: 5, borderRadius: 10 }, categoryPillText: { fontSize: 9.8, color: GREEN, fontWeight: '700' }, rating: { fontSize: 10.6, color: '#34423D' }, site: { fontSize: 10.6, color: '#44524C' },
  aboutCard: { borderWidth: 1, borderColor: '#DCE8E1', borderRadius: 13, padding: 14, gap: 11, backgroundColor: '#F8FBF9' }, aboutHeader: { gap: 5 }, aboutHint: { fontSize: 11.2, lineHeight: 17, color: '#58655F' }, aboutList: { gap: 9 }, aboutItem: { gap: 2 }, aboutLabel: { color: GREEN, fontSize: 9.5, fontWeight: '900', letterSpacing: .45, textTransform: 'uppercase' }, aboutValue: { color: '#1F3029', fontSize: 12.2, lineHeight: 18, fontWeight: '700' }, aboutLoading: { minHeight: 48, borderWidth: 1, borderColor: '#E2E9E5', borderRadius: 11, backgroundColor: '#FAFCFB', flexDirection: 'row', alignItems: 'center', gap: 10, paddingHorizontal: 13 }, aboutLoadingCopy: { flex: 1, color: '#58655F', fontSize: 11.5, lineHeight: 17 }, aboutError: { borderRadius: 10, backgroundColor: '#F6F8F7', color: '#58655F', fontSize: 11.2, lineHeight: 17, padding: 12 }, attribution: { color: '#68766F', fontSize: 9.8, lineHeight: 15 }, confirmationToggle: { minHeight: 54, borderWidth: 1, borderColor: '#DCE5E0', borderRadius: 11, backgroundColor: '#FFF', flexDirection: 'row', alignItems: 'center', gap: 11, padding: 11 }, confirmationToggleSelected: { borderColor: '#A9DCC6', backgroundColor: '#F1FAF5' }, confirmationBox: { width: 24, height: 24, borderRadius: 6, borderWidth: 1.5, borderColor: '#9EAAA4', alignItems: 'center', justifyContent: 'center', backgroundColor: '#FFF' }, confirmationBoxSelected: { borderColor: GREEN, backgroundColor: GREEN }, confirmationCheck: { color: '#FFF', fontSize: 14, fontWeight: '900' }, confirmationCopy: { flex: 1, gap: 2 }, confirmationTitle: { color: '#163128', fontSize: 11.8, lineHeight: 17, fontWeight: '900' }, confirmationHint: { color: '#5B6862', fontSize: 10.4, lineHeight: 15.5 },
  detailsCard: { borderWidth: 1, borderColor: '#E4E9E6', borderRadius: 13, padding: 14, gap: 11, backgroundColor: '#FFF' }, sectionTitle: { fontSize: 11.5, fontWeight: '900', color: '#1E2D27' }, detail: { flexDirection: 'row', gap: 10, alignItems: 'flex-start' }, detailIcon: { width: 22, fontSize: 16, color: '#172720' }, detailText: { flex: 1, fontSize: 11.5, lineHeight: 17.5, color: '#26352F' }, divider: { height: 1, backgroundColor: '#E9EDEB' }, chipRow: { flexDirection: 'row', flexWrap: 'wrap', gap: 7 }, chip: { backgroundColor: '#E3F3E9', paddingHorizontal: 11, paddingVertical: 7, borderRadius: 11 }, chipText: { fontSize: 9.8, color: GREEN, fontWeight: '700' },
  locationPanel: { borderWidth: 1, borderColor: '#DCE8E1', borderRadius: 13, padding: 14, gap: 10, backgroundColor: '#F8FBF9' }, locationHeaderRow: { flexDirection: 'row', alignItems: 'flex-start', gap: 10 }, locationHeaderCopy: { flex: 1, gap: 4 }, locationCheck: { width: 25, height: 25, borderRadius: 13, textAlign: 'center', lineHeight: 25, overflow: 'hidden', backgroundColor: GREEN, color: '#FFF', fontWeight: '900' }, locationHint: { fontSize: 11.2, lineHeight: 17, color: '#58655F' }, locationName: { fontSize: 13.5, lineHeight: 19, fontWeight: '900', color: '#153128' }, locationAddress: { fontSize: 11.5, lineHeight: 17.5, color: '#33453E' }, locationMarket: { fontSize: 10.7, lineHeight: 16, color: GREEN, fontWeight: '800' }, locationLink: { alignSelf: 'flex-start', minHeight: 36, justifyContent: 'center' }, locationList: { gap: 8 }, locationCard: { minHeight: 74, borderWidth: 1, borderColor: '#DFE7E2', borderRadius: 11, padding: 11, backgroundColor: '#FFF', flexDirection: 'row', alignItems: 'center', gap: 10 }, locationDot: { width: 9, height: 9, borderRadius: 5, backgroundColor: GREEN }, locationCardCopy: { flex: 1, gap: 3 }, chevron: { fontSize: 25, color: '#698078' }, locationSearchRow: { flexDirection: 'row', alignItems: 'stretch', gap: 8 }, locationInput: { flex: 1, minHeight: 48, borderWidth: 1, borderColor: '#DBE5DF', borderRadius: 10, backgroundColor: '#FFF', paddingHorizontal: 12, fontSize: 12.5, color: '#20312A' }, locationSearchButton: { minWidth: 74, borderRadius: 10, backgroundColor: GREEN, alignItems: 'center', justifyContent: 'center', paddingHorizontal: 13 }, locationSearchButtonText: { color: '#FFF', fontSize: 12, fontWeight: '900' }, locationError: { fontSize: 11.2, lineHeight: 17, color: '#A1251B' },
  missingBox: { padding: 13, borderRadius: 10, borderWidth: 1, borderColor: '#DDE8E1', backgroundColor: '#F3F8F5', gap: 5 }, missingTitle: { color: '#0A2F25', fontSize: 12, fontWeight: '900' }, missingText: { color: '#52615A', fontSize: 11.5, lineHeight: 18 },
  primary: { minHeight: 55, borderRadius: 10, backgroundColor: '#008B58', alignItems: 'center', justifyContent: 'center', shadowColor: '#00643F', shadowOpacity: .1, shadowRadius: 8, shadowOffset: { width: 0, height: 4 }, elevation: 2 }, primaryText: { color: '#FFF', fontSize: 14.5, fontWeight: '800' }, edit: { alignItems: 'center', minHeight: 44, justifyContent: 'center', paddingVertical: 3 }, editText: { color: GREEN, fontSize: 12.5, fontWeight: '800' },
  errorBox: { padding: 11, borderRadius: 9, backgroundColor: '#FDECEC' }, error: { fontSize: 12, color: '#A1251B' }, pressed: { opacity: .92, transform: [{ scale: .99 }] }, disabled: { opacity: .5 }, fieldLabel: { fontSize: 12, fontWeight: '800', marginBottom: 6, color: '#26352F' }, fieldInput: { minHeight: 48, borderWidth: 1, borderColor: '#E2E7E4', borderRadius: 10, paddingHorizontal: 12, paddingVertical: 10, fontSize: 13, color: '#21302A', backgroundColor: '#FFF' }, fieldInputMultiline: { minHeight: 88 },
});