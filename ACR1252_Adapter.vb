''' <summary>
''' Felica command module
''' </summary>
Public Module FelicaCommand

    Private ReadOnly BLOCK_SIZE = 16

    ''' <summary>
    ''' create felica command for writing
    ''' </summary>
    ''' <param name="idm"></param>
    ''' <param name="blockData"></param>
    ''' <param name="addLength"></param>
    ''' <returns></returns>
    Public Function CreatePacketForWrite(ByVal idm() As Byte, ByVal blockData() As Byte, ByVal addLength As Boolean, ByVal blocklist As Byte())

        Dim blocks As Integer = (blockData.Length + BLOCK_SIZE - 1) \ BLOCK_SIZE

        '3-byte block list
        If blocklist Is Nothing Then
            blocklist = New Byte((blocks * 3) - 1) {}
            For i As Integer = 0 To blocks - 1
                blocklist(i * 3) = CByte(&H0)
                blocklist(i * 3 + 1) = CByte(i)
                blocklist(i * 3 + 2) = CByte(&H4)
            Next
        End If

        Dim len As Integer = 13 + blocklist.Length + BLOCK_SIZE * blocks
        If addLength Then
            len += 1
        End If

        Dim packet As Byte() = New Byte(len - 1) {} '32
        Dim pos As Integer = 0
        If addLength Then
            packet(0) = CByte(len)
            pos = 1
        End If

        packet(pos) = &H8
        pos += 1

        'idm
        Array.Copy(idm, 0, packet, pos, 8)
        pos += 8
        'service count
        packet(pos) = &H1
        pos += 1
        'service code
        packet(pos) = &H9
        packet(pos + 1) = &H0
        pos += 2
        'block count
        packet(pos) = CByte(blocks)
        pos += 1
        'block list
        For i As Integer = 0 To blocklist.Length - 1
            packet(pos) = blocklist(i)
            pos += 1
        Next
        'block data
        Array.Copy(blockData, 0, packet, pos, blockData.Length)

        Return packet

    End Function

    ''' <summary>
    ''' create felica command for reading
    ''' </summary>
    ''' <param name="idm"></param>
    ''' <param name="blocks"></param>
    ''' <returns></returns>
    Public Function CreatePacketForRead(idm() As Byte, blocks As Integer, addLength As Boolean, blockList As Byte()) As Byte()
        '3-byte block list
        If blockList Is Nothing Then
            blockList = New Byte((blocks * 3) - 1) {}
            For i As Integer = 0 To blocks - 1
                blockList(i * 3) = CByte(&H0)
                blockList(i * 3 + 1) = CByte(i)
                blockList(i * 3 + 2) = CByte(&H4)
            Next
        End If


        Dim len As Integer = blockList.Length + 13 '13 in C#
        If addLength Then
            len += 1
        End If

        Dim packet As Byte() = New Byte(len - 1) {} 'todo fuji uses -1 scl uses 0
        Dim pos As Integer = 0
        If addLength Then
            packet(0) = CByte(len)
            pos = 1
        End If

        packet(pos) = &H6
        pos += 1

        'idm
        Array.Copy(idm, 0, packet, pos, 8)
        pos += 8
        'service count
        packet(pos) = &H1
        pos += 1
        'service code
        packet(pos) = &H9
        packet(pos + 1) = &H0
        pos += 2
        'block count
        packet(pos) = CByte(blocks)
        pos += 1
        'block list
        For i As Integer = 0 To blockList.Length - 1
            packet(pos) = blockList(i)
            pos += 1
        Next

        Return packet
    End Function

    ''' <summary>
    ''' gets block data from felica response packet
    ''' </summary>
    ''' <param name="response"></param>
    ''' <returns></returns>
    Public Function GetBlockData(response() As Byte)
        Dim minLen As Integer = 13

        If response.Length < minLen Then
            Return Nothing
        End If

        Dim blockCount As Integer = response(minLen - 1)
        Dim blockData As Byte() = New Byte(blockCount * BLOCK_SIZE - 1) {}
        If response.Length < minLen + blockData.Length Then
            Return Nothing
        End If

        Array.Copy(response, minLen, blockData, 0, blockData.Length)

        Return blockData
    End Function

End Module
''' <summary>
''' acr1252 reader overrides
''' </summary>
Public Class ACR1252_Adapter
    Inherits NfcAdapter
    Implements IAdapter

    ''' <summary>
    ''' writes smart tage command
    ''' </summary>
    ''' <param name="idm"></param>
    ''' <param name="command"></param>
    Public Overrides Sub Write(idm() As Byte, command As Byte(), blocklist() As Byte) Implements IAdapter.Write

        Dim felica As Byte() = FelicaCommand.CreatePacketForWrite(idm, command, False, blocklist)
        Dim apdu As Byte() = New Byte(6 + (felica.Length - 1)) {}
        apdu(0) = &HFF
        apdu(1) = &H0
        apdu(2) = &H0
        apdu(3) = &H0
        apdu(4) = CByte((felica.Length + 1))
        apdu(5) = CByte((felica.Length + 1))

        Array.Copy(felica, 0, apdu, 6, felica.Length)

        Dim response As Byte() = SendApdu(apdu)

    End Sub

    ''' <summary>
    ''' read smart tag command
    ''' </summary>
    ''' <param name="idm"></param>
    ''' <param name="blocks"></param>
    ''' <returns></returns>
    Public Overrides Function Read(idm() As Byte, blocks As Integer, blocklist() As Byte) As Byte() Implements IAdapter.Read
        Dim felica As Byte() = FelicaCommand.CreatePacketForRead(idm, blocks, False, blocklist)
        Dim apdu As Byte() = New Byte(6 + (felica.Length - 1)) {}

        apdu(0) = &HFF
        apdu(1) = &H0
        apdu(2) = &H0
        apdu(3) = &H0
        apdu(4) = CByte((felica.Length + 1))
        apdu(5) = CByte((felica.Length + 1))

        Array.Copy(felica, 0, apdu, 6, felica.Length)
        Dim response As Byte() = SendApdu(apdu)

        Return FelicaCommand.GetBlockData(response)

    End Function



    ''' <summary>
    ''' write smart tag command 
    ''' </summary>
    ''' <param name="idm"></param>
    ''' <param name="command"></param>
    Public Overrides Sub Write(idm() As Byte, command() As Byte) Implements IAdapter.Write
        Dim felica As Byte() = FelicaCommand.CreatePacketForWrite(idm, command, False, Nothing)
        Dim apdu As Byte() = New Byte(6 + (felica.Length - 1)) {}

        apdu(0) = &HFF
        apdu(1) = &H0
        apdu(2) = &H0
        apdu(3) = &H0
        apdu(4) = CByte((felica.Length + 1))
        apdu(5) = CByte((felica.Length + 1))

        Array.Copy(felica, 0, apdu, 6, felica.Length)
        Dim response As Byte() = SendApdu(apdu)


        SendApdu(apdu)
    End Sub

    ''' <summary>
    ''' read smart tag command
    ''' </summary>
    ''' <param name="idm"></param>
    ''' <param name="blocks"></param>
    ''' <returns></returns>
    Public Overrides Function Read(idm As Byte(), blocks As Integer) As Byte()
        Dim felica As Byte() = FelicaCommand.CreatePacketForRead(idm, blocks, False, Nothing)
        Dim apdu As Byte() = New Byte(6 + (felica.Length - 1)) {}

        apdu(0) = &HFF
        apdu(1) = &H0
        apdu(2) = &H0
        apdu(3) = &H0
        apdu(4) = CByte((felica.Length + 1))
        apdu(5) = CByte((felica.Length + 1))

        Array.Copy(felica, 0, apdu, 6, felica.Length)
        Dim response As Byte() = SendApdu(apdu)

        Return FelicaCommand.GetBlockData(response)
    End Function

End Class