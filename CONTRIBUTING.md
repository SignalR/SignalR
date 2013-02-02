## Filing issues

The github issue list is for bugs, not discussions. If you have a question you want to ask you have many alternatives:
- [The SignalR forum](http://forums.asp.net/1254.aspx/1?ASP+NET+SignalR)
- [The JabbR chat room](https://jabbr.net/#/rooms/signalr)
- [Stackoverflow](http://stackoverflow.com/questions/tagged/signalr)

When filing issues, try to give as much details as possible. The best way to get your bug fixed is to be as detailed as you can be about the problem.
Providing a minimal project with steps to reproduce the problem is ideal. Here are questions you can answer before you file a bug to make
sure you're not missing any important information.

1. Did you read the [documentation](https://github.com/SignalR/SignalR/wiki)?
2. Did you read the [FAQ](https://github.com/SignalR/SignalR/wiki/Faq)?
3. Did you include the snippet of broken code in the issue?
4. Can you reproduce the problem in a brand new project?
5. What are the *EXACT* steps to reproduce this problem?
6. What operating system are you using?
7. What version of IIS are you using?
8. What version of Visual Studio?

Github supports [markdown](http://github.github.com/github-flavored-markdown/), so when filing bugs make sure you check 
the formatting before clicking submit. 

## Contributing code

### Coding guidelines

We follow the [ASP.NET webstack coding guidelines](http://aspnetwebstack.codeplex.com/wikipage?title=CodingConventions)

### Project Workflow

Our workflow is loosely based on [Gitbub Flow](http://scottchacon.com/2011/08/31/github-flow.html). We actively develop in the **dev** branch. This means that all pull requests by contributors need to be developed and submitted to the dev branch.
The master branch always matches the current release on [nuget.org](http://nuget.org/packages/Microsoft.AspNet.SignalR/) and we also [tag](https://github.com/SignalR/SignalR/tags) each release.
When the end of a milestone is coming up, we create a branch called **release** to stabilize the build for the upcoming release.
The release is then merged into master and deleted and the cycle continues until the end of the next milestone.

### Submitting Pull requests

Make sure you can build the code. Familiarize yourself with the project workflow and our coding conventions. If you don't know what a pull request is
read this https://help.github.com/articles/using-pull-requests.

Before submitting a feature or substantial code contribution please discuss it with the team and ensure it follows the product roadmap. Note that all code submissions will be rigorously reviewed and tested by the ASP.NET SignalR Team, and only those that meet an extremely high bar for both quality and design/roadmap appropriateness will be merged into the source.

You will need to submit a Contributor License Agreement form before submitting your pull request. This needs to only be done once for any Microsoft OSS project. Download the Contributor License Agreement (CLA). Please fill in, sign, scan and email it to msopentech-cla@microsoft.com.
