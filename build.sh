#!/bin/bash

if [ ! -f packages/FAKE/tools/FAKE.exe]; then
	mono --runtime=v4.5 .nuget\nuget.exe install FAKE -OutputDirectory packages -ExcludeVersion
fi
mono --runtume=v4.5 packages/FAKE/tools/FAKE.exe build.fsx $@