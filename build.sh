#!/usr/bin/env bash
export EnableNuGetPackageRestore="true"
xbuild Build/Build.proj /t:GoMono
