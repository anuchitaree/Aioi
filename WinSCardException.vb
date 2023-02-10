Public Class WinSCardException
    Inherits Exception
    Public Sub New()
    End Sub

    Public Sub New(returnCode As Integer)
        _code = returnCode
    End Sub

    Private _code As Integer = 0

    Public Property ReturnCode() As Integer
        Get
            Return _code
        End Get
        Set
            _code = Value
        End Set
    End Property

End Class
Public Class UiLogEventArgs
    Inherits System.EventArgs

    Public Message As String

    Public Sub New(ByVal s As String)
        MyBase.New()
        Message = s

    End Sub

End Class