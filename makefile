all: clean compile

compile:
	xbuild SignalR.sln
	
clean:
	xbuild SignalR.sln /t:Clean
	
# For some odd reason, this hangs after running all the tests so 
# ctrl + c might be needed to break out after tests run.
tests: compile
	xbuild Build/Build.proj /t:RunTests