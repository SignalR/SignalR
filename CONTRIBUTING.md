## Filing issues

The github issue list is for bugs, not discussions. If you have a question you want to ask you have many alternatives:
- [The SignalR forum](http://forums.asp.net/1254.aspx/1?ASP+NET+SignalR)
- [The JabbR chat room](https://jabbr.net/#/rooms/signalr)
- [Stackoverflow](http://stackoverflow.com/questions/tagged/signalr)

When filing issues, please use our [bug filing templates](https://gist.github.com/signalrcoreteam/5317001). The best way to get your bug fixed is to be as detailed as you can be about the problem.
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

Our workflow is loosely based on [Github Flow](http://scottchacon.com/2011/08/31/github-flow.html). We actively develop in the **dev** branch. This means that all pull requests by contributors need to be developed and submitted to the dev branch.
The master branch always matches the current release on [nuget.org](http://nuget.org/packages/Microsoft.AspNet.SignalR/) and we also [tag](https://github.com/SignalR/SignalR/tags) each release.
When the end of a milestone is coming up, we create a branch called **release** to stabilize the build for the upcoming release.
The release is then merged into master and deleted and the cycle continues until the end of the next milestone.

### Issue management

**Tag Format**

- **Bug** – A Bug
- **Feature** - A Feature
- **Task** – Has no effect on product code

**States**

- **Ready** – Bug/Feature ready to be worked on
- **Working** – In process of development
- **Review** – Bug/Feature is coded and now needs to be reviewed before being "accepted" as final
- **Done** (Open) – Bug/Feature has been "accepted" as final and is ready for verification (pushed to source)
- **Done** (Closed) – Bug/Feature has been "accepted" as final and has resolved the corresponding issue

### Submitting Pull requests

You will need to sign a [Contributor License Agreement](https://cla.msopentech.com) before submitting your pull request. To complete the Contributor License Agreement (CLA), you will need to submit a request via the form and then electronically sign the Contributor License Agreement when you receive the email containing the link to the document. This needs to only be done once for any Microsoft Open Technologies OSS project.

Make sure you can build the code. Familiarize yourself with the project workflow and our coding conventions. If you don't know what a pull request is
read this https://help.github.com/articles/using-pull-requests.

Before submitting a feature or substantial code contribution please discuss it with the team and ensure it follows the product roadmap. Note that all code submissions will be rigorously reviewed and tested by the ASP.NET SignalR Team, and only those that meet an extremely high bar for both quality and design/roadmap appropriateness will be merged into the source.
[Don't "Push" Your Pull Requests](http://www.igvita.com/2011/12/19/dont-push-your-pull-requests/)

Here's a few things you should always do when making changes to the SignalR code base:

**Commit/Pull Request Format**

```
Summary of the changes (Less than 80 chars)
 - Detail 1
 - Detail 2

#bugnumber (in this specific format)
```

**Tests**

-  Tests need to be provided for every bug/feature that is completed.
-  Tests only need to be present for issues that need to be verified by QA (e.g. not tasks)
-  If there is a scenario that is far too hard to test there does not need to be a test for it.
   - "Too hard" is determined by the team as a whole.
