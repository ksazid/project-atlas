#!/usr/bin/env sh
set -eu

if [ "${ATLAS_RUN_MIGRATIONS:-false}" = "true" ]; then
  /app/efbundle --connection "${ConnectionStrings__Atlas}"
fi

exec dotnet /app/Atlas.Api.dll
