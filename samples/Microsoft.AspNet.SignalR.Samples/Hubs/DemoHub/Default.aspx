<%@ Page Title="ASP.NET SignalR: Demo Hub" Language="C#" MasterPageFile="~/SignalR.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="Microsoft.AspNet.SignalR.Samples.Hubs.DemoHub.Default" %>

<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <ul class="breadcrumb">
        <li><a href="<%: ResolveUrl("~/") %>">SignalR Samples</a> <span class="divider">/</span></li>
        <li class="active">Demo Hub</li>
    </ul>

    <div class="page-header">
        <h2>Demo Hub</h2>
        <p>A contrived example that exploits every feature of the Hub API.</p>
    </div>

    <dl class="dl-expanded">
        <dt>Arbitrary Code</dt>
        <dd id="arbitraryCode"></dd>
        
        <dt>Report Progress</dt>
        <dd id="progress">not started</dd>

        <dt>Group Added</dt>
        <dd id="groupAdded"></dd>
        
        <dt>Generic Task</dt>
        <dd id="value"></dd>
        
        <dt>Task With Exception</dt>
        <dd id="taskWithException"></dd>
        
        <dt>Generic Task With Exception</dt>
        <dd id="genericTaskWithException"></dd>

        <dt>Synchronous Exception</dt>
        <dd id="synchronousException"></dd>
        
        <dt>Dynamic Task</dt>
        <dd id="dynamicTask"></dd>
        
        <dt>Invoking hub method with dynamic</dt>
        <dd id="passingDynamicComplex"></dd>
        
        <dt>SimpleArray</dt>
        <dd id="simpleArray"></dd>
        
        <dt>ComplexType</dt>
        <dd id="complexType"></dd>

        <dt>ComplexArray</dt>
        <dd id="complexArray"></dd>
        
        <dt>Overloads</dt>
        <dd id="overloads"></dd>

        <dt>Read State Value</dtRead>
        <dd id="readStateValue"></dd>
        
        <dt>Inline Script Tag</dtRead>
        <dd id="inlineScriptTag"></dd>

        <dt>Plain Task</dt>
        <dd id="plainTask"></dd>

        <dt>Generic Task With ContinueWith</dt>
        <dd id="genericTaskWithContinueWith"></dd>
        
        <dt>Typed callback</dt>
        <dd id="typed"></dd>

        <dt>Message Pump</dt>
        <dd id="msg"></dd>
    </dl>
</asp:Content>

<asp:Content ContentPlaceHolderID="Scripts" runat="server">
    <script src="<%: ResolveUrl("~/signalr/js") %>"></script>
    <script>
        var options = { transport: activeTransport };
    </script>
    <script src="DemoHub.js"></script>
</asp:Content>
