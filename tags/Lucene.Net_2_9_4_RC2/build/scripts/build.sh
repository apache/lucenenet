#!/bin/bash
# Licensed to the Apache Software Foundation (ASF) under one or more
# contributor license agreements.  See the NOTICE file distributed with
# this work for additional information regarding copyright ownership.
# The ASF licenses this file to You under the Apache License, Version 2.0
# (the "License"); you may not use this file except in compliance with
# the License.  You may obtain a copy of the License at
#  
# http://www.apache.org/licenses/LICENSE-2.0
#  
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

TARGETS="all"
BuildArea="all"
Configuration="debug"
if [ -n "$1" ] 
		then 
			TARGETS=$1
fi
if [ "$#" -gt "1" ]
		then
			TARGETS=${!#}
fi
if [ $# -eq 2 ]
		then
			BuildArea="$1"
fi 
if [ $# -eq 3 ]
		then
			BuildArea="$1"
			Configuration="$2"
fi

echo "commands will target projects: $BuildArea"
echo "commands will target the configuration: $Configuration"
export $BuildArea
export $Configuration

ROOT=$(dirname $0)
export NETFRAMEWORK="mono"
export TEMP=$ROOT/tmp

MONO_IOMAP=case xbuild $ROOT/build.xml /t:$TARGETS