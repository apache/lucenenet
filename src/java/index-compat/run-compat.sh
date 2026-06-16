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

# Thin wrapper that forwards to run-compat.ps1, which contains all the business
# logic. This keeps the cross-platform logic in a single place; the .sh, .bat,
# and .ps1 entry points differ only in how they locate and invoke PowerShell.
#
# See run-compat.ps1 for what the harness does (both directions of the Lucene
# 4.8.1 <-> Lucene.NET index compatibility check, issue #270) and the
# COMPAT_TFM environment variable.

if ! command -v pwsh &> /dev/null
then
    echo "PowerShell Core could not be found. Please install version 3 or higher."
    exit 1
fi

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
pwsh -ExecutionPolicy bypass -Command "& '$HERE/run-compat.ps1'" "$@"
