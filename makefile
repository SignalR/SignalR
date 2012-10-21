all: clean compile

compile:
	xbuild Microsoft.AspNet.SignalR.Mono.sln
	
clean:
	xbuild Microsoft.AspNet.SignalR.Mono.sln /t:Clean
	
# For some odd reason, this hangs after running all the tests so 
# ctrl + c might be needed to break out after tests run.
tests: compile
	xbuild Build/Build.proj /t:RunTests