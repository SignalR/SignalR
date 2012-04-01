task :default do
  Dir["*/"]+Dir["samples/*/"].map do |dir|
    puts "Restore packages for '#{dir}'"
    sh "mono --runtime='v4.0' .nuget/NuGet.exe install #{dir}/packages.config -o 'packages'" if File::exists?("#{dir}/packages.config")
  end
end