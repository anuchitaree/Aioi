
Imports System.Threading
''' <summary>
''' Default SCL010 Read-Writer
''' </summary>
Public Class NfcAdapter
    Implements IAdapter

    Private Shared ReadOnly RECV_BUFF_SIZE As Integer = 256
    Public Shared ReadOnly BLOCK_SIZE As Integer = 16

    Private _protocol As Integer = 0
    Private _cardHandle As Integer = 0
    Private _recvBuff As Byte() = New Byte(RECV_BUFF_SIZE - 1) {}
    Private _pioSendRequest As PCSCModules.SCARD_IO_REQUEST
    Private _readerState As PCSCModules.SCARD_READERSTATE
    Private _context As Integer
    Private _name As String
    Private _retryCount As Integer = 5

    ''' <summary>
    ''' Sets or gets the retry count.
    ''' </summary>
    Public Property RetryCount() As Integer
        Get
            Return _retryCount
        End Get
        Set
            _retryCount = Value
        End Set
    End Property

    Protected Function SendApdu(command As Byte()) As Byte()
        Dim recvLen As Integer = 255

        For i As Integer = 0 To RetryCount

            Dim ret As Integer = PCSCModules.SCardTransmit(_cardHandle, _pioSendRequest, command(0),
                                                       command.Length, _pioSendRequest, _recvBuff(0), recvLen)

            If ret <> PCSCModules.SCARD_S_SUCCESS Then
                Throw New WinSCardException(ret)
            Else
                Exit For
            End If
        Next
        Dim response As Byte() = New Byte(recvLen - 1) {}
        Array.Copy(_recvBuff, response, recvLen)

        Return response
    End Function

    Public Property Context As Integer Implements IAdapter.Context
        Get
            Return _context
        End Get
        Set(value As Integer)
            _context = value
        End Set
    End Property

    Public Property Name As String Implements IAdapter.Name
        Get
            Return _name
        End Get
        Set(value As String)
            _name = value
        End Set
    End Property

    ''' <summary>
    ''' opens touched smart tag
    ''' </summary>
    Public Sub OpenCard() Implements IAdapter.OpenCard

        Dim ret As Integer = -1

        For i As Integer = 0 To RetryCount
            ret = PCSCModules.SCardConnect(Me.Context, Me.Name, PCSCModules.SCARD_SHARE_EXCLUSIVE,
               PCSCModules.SCARD_PROTOCOL_T0 Or PCSCModules.SCARD_PROTOCOL_T1, _cardHandle, _protocol)

            If ret = 0 Then
                Exit For

            End If
            Thread.Sleep(100)
            Console.WriteLine("OpenCard: Retry")
        Next

        If ret <> PCSCModules.SCARD_S_SUCCESS Then
            Throw New WinSCardException(ret)
        End If
        _pioSendRequest.dwProtocol = _protocol
        _pioSendRequest.cbPciLength = 8
    End Sub

    Public Function Poll() As Boolean Implements IAdapter.Poll
        _readerState.RdrName = Me.Name
        _readerState.RdrCurrState = PCSCModules.SCARD_STATE_UNAVAILABLE

        Dim ret As Integer = PCSCModules.SCardGetStatusChange(Me.Context, 100, _readerState, 1)
        If ret <> PCSCModules.SCARD_S_SUCCESS Then
            Return False
        End If

        If (_readerState.RdrEventState And PCSCModules.SCARD_STATE_PRESENT) <> 0 Then
            Return True
        End If
        Return False
    End Function

    ''' <summary>
    ''' gets idm
    ''' </summary>
    ''' <returns></returns>
    Public Function GetIdm() As Byte() Implements IAdapter.GetIdm
        Dim apdu As Byte() = New Byte() {&HFF, &HCA, &H0, &H0, &H0}

        Dim res As Byte() = SendApdu(apdu)

        If res IsNot Nothing AndAlso res.Length > 8 Then
            Dim idm As Byte() = New Byte(7) {}
            Array.Copy(res, 0, idm, 0, idm.Length)
            Return idm
        Else
            Return Nothing
        End If

    End Function

    ''' <summary>
    ''' close touched smart tag
    ''' </summary>
    ''' <returns></returns>
    Public Function CloseCard() As Integer Implements IAdapter.CloseCard
        Return PCSCModules.SCardDisconnect(_cardHandle, PCSCModules.SCARD_LEAVE_CARD)

    End Function

    ''' <summary>
    ''' writes smart tage command
    ''' </summary>
    ''' <param name="idm"></param>
    ''' <param name="command"></param>
    Public Overridable Sub Write(idm() As Byte, command() As Byte) Implements IAdapter.Write
        Dim felica As Byte() = FelicaCommand.CreatePacketForWrite(idm, command, False, Nothing)
        Dim apdu As Byte() = New Byte(6 + (felica.Length - 1)) {}
        apdu(0) = &HFF
        apdu(1) = &HCC
        apdu(2) = &H0
        apdu(3) = &H0
        apdu(4) = CByte((felica.Length + 1))
        apdu(5) = &HF3

        Array.Copy(felica, 0, apdu, 6, felica.Length)

        SendApdu(apdu)

    End Sub

    ''' <summary>
    ''' read smart tag command
    ''' </summary>
    ''' <param name="idm"></param>
    ''' <param name="blocks"></param>
    ''' <returns></returns>
    Public Overridable Function Read(idm() As Byte, blocks As Integer) As Byte() Implements IAdapter.Read
        Dim felica As Byte() = FelicaCommand.CreatePacketForRead(idm, blocks, False, Nothing)
        Dim apdu As Byte() = New Byte(6 + (felica.Length)) {}

        apdu(0) = &HFF
        apdu(1) = &HCC
        apdu(2) = &H0
        apdu(3) = &H0
        apdu(4) = CByte((felica.Length + 1))
        apdu(5) = &HF3

        Array.Copy(felica, 0, apdu, 6, felica.Length)
        Dim response As Byte() = SendApdu(apdu)

        Return FelicaCommand.GetBlockData(response)

    End Function

    ''' <summary>
    ''' not implemented
    ''' </summary>
    Public Sub OpenAdapter() Implements IAdapter.OpenAdapter
        ' Not Applicable
    End Sub

    ''' <summary>
    ''' not implemented
    ''' </summary>
    ''' <returns></returns>
    Public Function CloseAdapter() As Integer Implements IAdapter.CloseAdapter
        'not applicable
        Return PCSCModules.SCARD_S_SUCCESS

    End Function

    Public Overridable Sub Write(idm() As Byte, command() As Byte, blocklist() As Byte) Implements IAdapter.Write
        Dim felica As Byte() = FelicaCommand.CreatePacketForWrite(idm, command, False, blocklist)
        Dim apdu As Byte() = New Byte(6 + (felica.Length - 1)) {}
        apdu(0) = &HFF
        apdu(1) = &HCC
        apdu(2) = &H0
        apdu(3) = &H0
        apdu(4) = CByte((felica.Length + 1))
        apdu(5) = &HF3

        Array.Copy(felica, 0, apdu, 6, felica.Length)

        SendApdu(apdu)

    End Sub

    Public Overridable Function Read(idm() As Byte, blocks As Integer, blocklist() As Byte) As Byte() Implements IAdapter.Read
        Dim felica As Byte() = FelicaCommand.CreatePacketForRead(idm, blocks, False, blocklist)
        Dim apdu As Byte() = New Byte(6 + (felica.Length)) {}

        apdu(0) = &HFF
        apdu(1) = &HCC
        apdu(2) = &H0
        apdu(3) = &H0
        apdu(4) = CByte((felica.Length + 1))
        apdu(5) = &HF3

        Array.Copy(felica, 0, apdu, 6, felica.Length)
        Dim response As Byte() = SendApdu(apdu)

        Return FelicaCommand.GetBlockData(response)

    End Function
End Class
