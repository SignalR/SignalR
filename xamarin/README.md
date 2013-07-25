# Xamarin Component Sample

This is a sample template that you can use as a guide to create your own
components.

To build this sample component:

```shell
# Download xpkg
curl -L https://components.xamarin.com/submit/xpkg > xpkg.zip
mkdir xpkg
unzip -o -d xpkg xpkg.zip

# Create the component package
mono xpkg/xamarin-component.exe create sample-component-1.0.xam \
    --name="My Awesome Component" \
    --summary="Add a huge amount of awesomeness to your Xamarin apps." \
    --publisher="Awesome Corp, Inc." \
    --website="http://awesomecorp.com/component" \
    --details="Details.md" \
    --license="License.md" \
    --getting-started="GettingStarted.md" \
    --icon="icons/Awesome_128x128.png" \
    --icon="icons/Awesome_512x512.png" \
    --library="ios":"bin/Awesome.iOS.dll" \
    --library="android":"bin/Awesome.Android.dll" \
    --sample="iOS Sample. Demonstrates Awesomeness on iOS.":"samples/Awesome.iOS.sln" \
    --sample="Android Sample. Demonstrates Awesomeness on Android":"samples/Awesome.Android.sln"
```

There's a Rakefile in this repo that will do these steps for you if you
simply type `rake`:

```shell
$ rake
* Downloading xpkg...
* Creating sample-component-1.0.xam...
mono xpkg/xamarin-component.exe create sample-component-1.0.xam \
    --name="My Awesome Component" \
    --summary="Add a huge amount of awesomeness to your Xamarin apps." \
    --publisher="Awesome Corp, Inc." \
    --website="http://awesomecorp.com/component" \
    --details="Details.md" \
    --license="License.md" \
    --getting-started="GettingStarted.md" \
    --icon="icons/Awesome_128x128.png" \
    --icon="icons/Awesome_512x512.png" \
    --library="ios":"bin/Awesome.iOS.dll" \
    --library="android":"bin/Awesome.Android.dll" \
    --sample="iOS Sample. Demonstrates Awesomeness on iOS.":"samples/Awesome.iOS.sln" \
    --sample="Android Sample. Demonstrates Awesomeness on Android":"samples/Awesome.Android.sln"
* Created sample-component-1.0.xam
```
