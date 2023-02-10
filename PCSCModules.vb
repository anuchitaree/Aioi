Imports System.Runtime.InteropServices
Imports System.Text

Public Class PCSCModules
    Public Const SCARD_SCOPE_USER As Integer = 0
    Public Const SCARD_S_SUCCESS As Integer = 0

    Public Const SCARD_SHARE_EXCLUSIVE As Integer = 1
    Public Const SCARD_SHARE_SHARED As Integer = 2
    Public Const SCARD_SHARE_DIRECT As Integer = 3

    Public Const SCARD_LEAVE_CARD As Integer = 0
    Public Const SCARD_RESET_CARD As Integer = 1
    Public Const SCARD_UNPOWER_CARD As Integer = 2
    Public Const SCARD_EJECT_CARD As Integer = 3

    Public Const SCARD_PROTOCOL_UNDEFINED As Integer = &H0
    Public Const SCARD_PROTOCOL_T0 As Integer = &H1
    Public Const SCARD_PROTOCOL_T1 As Integer = &H2
    Public Const SCARD_PROTOCOL_RAW As Integer = &H10000

    Public Const SCARD_STATE_UNKNOWN As Integer = &H4
    Public Const SCARD_STATE_UNAVAILABLE As Integer = &H8
    Public Const SCARD_STATE_EMPTY As Integer = &H10
    Public Const SCARD_STATE_PRESENT As Integer = &H20
    Public Const SCARD_STATE_ATRMATCH As Integer = &H40
    Public Const SCARD_STATE_EXCLUSIVE As Integer = &H80
    Public Const SCARD_STATE_INUSE As Integer = &H100
    Public Const SCARD_STATE_MUTE As Integer = &H200
    Public Const SCARD_STATE_UNPOWERED As Integer = &H400

    <StructLayout(LayoutKind.Sequential)>
    Public Structure SCARD_IO_REQUEST
        Public dwProtocol As IntPtr
        Public cbPciLength As IntPtr
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Public Structure APDURec
        Public bCLA As Byte
        Public bINS As Byte
        Public bP1 As Byte
        Public bP2 As Byte
        Public bP3 As Byte
        <MarshalAs(UnmanagedType.ByValArray, SizeConst:=256)>
        Public Data As Byte()
        <MarshalAs(UnmanagedType.ByValArray, SizeConst:=3)>
        Public SW As Byte()
        Public IsSend As Boolean
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Public Structure SCARD_READERSTATE
        Public RdrName As String
        Public UserData As Integer
        Public RdrCurrState As Integer
        Public RdrEventState As Integer
        Public ATRLength As Integer
        <MarshalAs(UnmanagedType.ByValArray, SizeConst:=37)>
        Public ATRValue As Byte()
    End Structure

    <DllImport("winscard.dll")>
    Public Shared Function SCardEstablishContext(dwScope As UInteger, pvReserved1 As Integer, pvReserved2 As Integer, ByRef phContext As Integer) As Integer
    End Function

    <DllImport("winscard.dll")>
    Public Shared Function SCardReleaseContext(phContext As Integer) As Integer
    End Function

    <DllImport("winscard.dll")>
    Public Shared Function SCardConnect(hContext As Integer, szReaderName As String, dwShareMode As Integer, dwPrefProtocol As Integer, ByRef phCard As Integer, ByRef ActiveProtocol As Integer) As Integer
    End Function

    <DllImport("winscard.dll")>
    Public Shared Function SCardBeginTransaction(hCard As Integer) As Integer
    End Function

    <DllImport("winscard.dll")>
    Public Shared Function SCardDisconnect(hCard As Integer, Disposition As Integer) As Integer
    End Function

    <DllImport("winscard.DLL", EntryPoint:="SCardListReadersA", CharSet:=CharSet.Ansi)>
    Public Shared Function SCardListReaders(hContext As Integer, Groups As Byte(), Readers As Byte(), ByRef pcchReaders As Integer) As Integer
    End Function

    <DllImport("winscard.dll")>
    Public Shared Function SCardStatus(hCard As Integer, szReaderName As String, ByRef pcchReaderLen As Integer, ByRef State As Integer, ByRef Protocol As Integer, ByRef ATR As Byte,
    ByRef ATRLen As Integer) As Integer
    End Function

    <DllImport("winscard.dll")>
    Public Shared Function SCardEndTransaction(hCard As Integer, Disposition As Integer) As Integer
    End Function

    <DllImport("winscard.dll")>
    Public Shared Function SCardState(hCard As Integer, ByRef State As UInteger, ByRef Protocol As UInteger, ByRef ATR As Byte, ByRef ATRLen As UInteger) As Integer
    End Function

    <DllImport("winscard.dll")>
    Public Shared Function SCardTransmit(hCard As IntPtr, ByRef pioSendRequest As SCARD_IO_REQUEST, ByRef SendBuff As Byte, SendBuffLen As IntPtr, ByRef pioRecvRequest As SCARD_IO_REQUEST, ByRef RecvBuff As Byte, ByRef RecvBuffLen As Integer) As Integer
    End Function

    <DllImport("winscard.dll")>
    Public Shared Function SCardControl(hCard As Integer, dwControlCode As UInteger, ByRef SendBuff As Byte, SendBuffLen As Integer, ByRef RecvBuff As Byte, RecvBuffLen As Integer,
        ByRef pcbBytesReturned As Integer) As Integer
    End Function

    <DllImport("winscard.dll")>
    Public Shared Function SCardGetStatusChange(hContext As Integer, TimeOut As Integer, ByRef ReaderState As SCARD_READERSTATE, ReaderCount As Integer) As Integer
    End Function



    ''' <summary>
    ''' returns pc/sc context
    ''' </summary>
    ''' <param name="context"></param>
    ''' <returns></returns>
    Public Shared Function GetContext(ByRef context As Integer) As Integer
        context = 0
        Return SCardEstablishContext(SCARD_SCOPE_USER, 0, 0, context)
    End Function

    ''' <summary>
    ''' returns device name list
    ''' </summary>
    ''' <param name="context"></param>
    ''' <returns></returns>
    Public Shared Function GetAdapters(context As Integer) As String()
        Dim pcchReaders As Integer = 0
        Dim ret As Integer = SCardListReaders(context, Nothing, Nothing, pcchReaders)

        If ret <> SCARD_S_SUCCESS Then
            Return Nothing
        End If

        Dim buffer As Byte() = New Byte(pcchReaders - 1) {}
        ret = SCardListReaders(context, Nothing, buffer, pcchReaders)

        If ret <> SCARD_S_SUCCESS Then
            Return Nothing
        End If

        Dim nameSerial As String = Encoding.ASCII.GetString(buffer)
        Return nameSerial.Split(New Char() {ControlChars.NullChar}, StringSplitOptions.RemoveEmptyEntries)
    End Function

    ''' <summary>
    ''' Releases the context.
    ''' </summary>
    Public Shared Sub ReleaseContext(context As Integer)
        SCardReleaseContext(context)
    End Sub

End Class