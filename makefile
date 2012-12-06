all: 
	xbuild Build/Build.proj /t:GoMono

compile:
	xbuild Build/Build.proj /t:Build
	
# For some odd reason, this hangs after running all the tests so 
# ctrl + c might be needed to break out after tests run.
tests: compile
	xbuild Build/Build.proj /t:RunTests

functionaltests: compile
    xbuild Build/Build.proj /t:RunFunctionalTests
