#!/usr/bin/env bash
# Licensed to the Apache Software Foundation (ASF) under one
# or more contributor license agreements.  See the NOTICE file
# distributed with this work for additional information
# regarding copyright ownership.  The ASF licenses this file
# to you under the Apache License, Version 2.0 (the
# "License"); you may not use this file except in compliance
# with the License.  You may obtain a copy of the License at
#
#   http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing,
# software distributed under the License is distributed on an
# "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
# KIND, either express or implied.  See the License for the
# specific language governing permissions and limitations
# under the License.

# Runs both directions of the Lucene 4.8.1 <-> Lucene.NET index compatibility
# check (issue #270). Requires a JDK and the .NET SDK. All generated indexes go
# under the gitignored work/ folder; nothing is committed.
#
# By default the test shard builds with its own default target framework. Set
# COMPAT_TFM (e.g. net10.0, net8.0) to force a specific one.

set -euo pipefail

# Guarantee a clear message and non-zero exit code on any failure so CI fails the job.
trap 'status=$?; if [[ $status -ne 0 ]]; then echo "==> Compatibility check FAILED (exit $status)" >&2; fi' EXIT

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$HERE/../../.." && pwd)"
WORK="$HERE/work"
SHARD="$REPO_ROOT/src/Lucene.Net.Tests._I-J/Lucene.Net.Tests._I-J.csproj"

JAVA_INDEX="$WORK/java"
DOTNET_INDEX="$WORK/dotnet"

# Only pass -f when the caller explicitly forces a target framework.
TFM_ARGS=()
if [[ -n "${COMPAT_TFM:-}" ]]; then
  TFM_ARGS=(-f "$COMPAT_TFM")
fi

# dotnet test exits 0 when a --filter matches nothing, which would hide a
# misconfiguration. Capture output, surface it, and fail on "matched 0".
# Env-var names contain dots, which bash forbids as bare assignments, so the
# caller passes "name=value" pairs that we apply via `env`.
run_dotnet_test() {
  local filter="$1"
  shift
  local out
  if ! out="$(env "$@" dotnet test "$SHARD" ${TFM_ARGS[@]+"${TFM_ARGS[@]}"} -c Release --no-build --filter "$filter" 2>&1)"; then
    echo "$out"
    echo "ERROR: dotnet test failed for filter '$filter'" >&2
    return 1
  fi
  echo "$out"
  if echo "$out" | grep -Eq 'no test is available|matches the given testcase filter|total:[[:space:]]*0\b'; then
    echo "ERROR: No tests ran for filter '$filter'. Is the shard built for this target framework?" >&2
    return 1
  fi
}

echo "==> Building the .NET test shard${COMPAT_TFM:+ ($COMPAT_TFM)}"
dotnet build "$SHARD" ${TFM_ARGS[@]+"${TFM_ARGS[@]}"} -c Release

echo
echo "==> Direction 1: .NET writes, Java reads"
echo "    .NET writing index into $DOTNET_INDEX"
run_dotnet_test "FullyQualifiedName~TestJavaCompatibility.TestWriteIndexForJava" \
  "lucenenet.compat.write.dir=$DOTNET_INDEX"

for variant in index.481.nocfs index.481.cfs; do
  echo "    Java reading $DOTNET_INDEX/$variant"
  (cd "$HERE" && ./mvnw -q test -Dlucenenet.index.dir="$DOTNET_INDEX/$variant")
done

echo
echo "==> Direction 2: Java writes, .NET reads"
echo "    Java writing index into $JAVA_INDEX"
(cd "$HERE" && ./mvnw -q compile exec:java -Dexec.args="$JAVA_INDEX")

echo "    .NET reading from $JAVA_INDEX"
run_dotnet_test "FullyQualifiedName~TestJavaCompatibility.TestReadJavaIndex" \
  "lucenenet.compat.read.dir=$JAVA_INDEX"

echo
echo "==> Both directions passed."
