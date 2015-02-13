Imports System

Module Main

    Sub Main()
        Dim writer = Console.Out
        Dim client = New Client(writer)
        'Connection string
        client.Run("http://localhost:8080/")
    End Sub

End Module
