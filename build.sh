#!/usr/bin/env bash
export EnableNuGetPackageRestore="true"
xbuild build/Build.proj /t:GoMono
