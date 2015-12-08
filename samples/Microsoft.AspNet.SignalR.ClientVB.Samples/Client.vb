Imports System
Imports System.IO
Imports System.Text
Imports Microsoft.AspNet.SignalR.Client
Imports Microsoft.AspNet.SignalR.Hubs

Public Class Client
    Private _traceWriter As TextWriter
    Public Sub New(writer As TextWriter)
        _traceWriter = writer
    End Sub
    Public Sub Run(url As String)
        Try
            Dim HubConnection = New HubConnection(url)
            HubConnection.TraceWriter = _traceWriter
            HubConnection.TraceLevel = TraceLevels.Events

            Dim hubProxy = HubConnection.CreateHubProxy("chat")
            'Register client function and behavior
            hubProxy.On(Of String)("send", Sub(data) HubConnection.TraceWriter.WriteLine(data))

            HubConnection.Start().Wait()
            HubConnection.TraceWriter.WriteLine("transport.Name={0}", HubConnection.Transport.Name)

            'User input loop
            Dim send As String = ""
            While send <> "/q"
                hubProxy.Invoke("Send", send).Wait()
                send = Console.ReadLine()
            End While

            '-----Examples-----
            Dim joinGroupResponse = hubProxy.Invoke(Of String)("JoinGroup", HubConnection.ConnectionId, "CommonClientGroup").Result
            HubConnection.TraceWriter.WriteLine("joinGroupResponse={0}", joinGroupResponse)

            hubProxy.Invoke("DisplayMessageGroup", "CommonClientGroup", "Hello Group Members!").Wait()

            Dim leaveGroupResponse = hubProxy.Invoke(Of String)("LeaveGroup", HubConnection.ConnectionId, "CommonClientGroup").Result
            HubConnection.TraceWriter.WriteLine("leaveGroupResponse={0}", leaveGroupResponse)

            hubProxy.Invoke("DisplayMessageGroup", "CommonClientGroup", "Hello Group Members! (caller should not see this message)").Wait()

            hubProxy.Invoke("DisplayMessageCaller", "Hello Caller again!").Wait()

        Catch ex As Exception
            _traceWriter.WriteLine("Exception: {0}", ex)
        End Try
    End Sub
End Class
