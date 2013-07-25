require "rake/clean"

CLEAN.include "*.xam"
CLEAN.include "xamarin-component"

COMPONENT = "awesome-1.0.xam"

file "xamarin-component/xamarin-component.exe" do
	puts "* Downloading xamarin-component..."
	mkdir "xamarin-component"
	sh "curl -L https://components.xamarin.com/submit/xpkg > xamarin-component.zip"
	sh "unzip -o xamarin-component.zip -d xamarin-component"
	sh "rm xamarin-component.zip"
end

task :default => "xamarin-component/xamarin-component.exe" do
	line = <<-END
	mono xamarin-component/xamarin-component.exe create-manually #{COMPONENT} \
		--name="My Awesome Component" \
		--summary="Add a huge amount of awesomeness to your Xamarin apps." \
		--publisher="Awesome Corp, Inc." \
		--website="http://awesomecorp.com/component" \
		--details="Details.md" \
		--license="License.md" \
		--getting-started="GettingStarted.md" \
		--icon="icons/awesome_128x128.png" \
		--icon="icons/awesome_512x512.png" \
		--library="ios":"bin/Awesome.iOS.dll" \
		--library="android":"bin/Awesome.Android.dll" \
		--sample="iOS Sample. Demonstrates Awesomeness on iOS.":"samples/Awesome.iOS.sln" \
		--sample="Android Sample. Demonstrates Awesomeness on Android":"samples/Awesome.Android.sln"
		END
	puts "* Creating #{COMPONENT}..."
	puts line.strip.gsub "\t\t", "\\\n    "
	sh line, :verbose => false
	puts "* Created #{COMPONENT}"
end
