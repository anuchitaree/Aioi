Imports System.Text
Imports System.Threading
Imports AioiSystems.DotModule
Imports AioiSystems.SmartTag
Imports CommandBuilder

Public Class SmartTag
    Public Sub New(adapter As IAdapter)
        _adapter = adapter
        _builder = New CommandBuilder()
        _builder.setMaxBlocks(12)
    End Sub
    ''' <summary>
    ''' log event to write to form
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="args"></param>
    Public Event ShowMessage(ByVal sender As Object, ByVal args As UiLogEventArgs)
    Public Sub WriteToUiLog(message As String)
        Dim args As New UiLogEventArgs(message)
        RaiseEvent ShowMessage(Me, args)
    End Sub

    Public Enum SmartTagFunctions
        [Nothing]
        ShowImage
        ClearDisplay
        WriteUserData
        ReadUserData
        WriteToCardArea
        ReadFromCardArea
    End Enum

    Public Shared ReadOnly TAG_STS_UNKNOWN As Byte = &H70
    Public Shared ReadOnly TAG_STS_INIT As Byte = 0
    Public Shared ReadOnly TAG_STS_PROCESSED As Byte = &HF0
    Public Shared ReadOnly TAG_STS_BUSY As Byte = &HF2
    Private Shared ReadOnly MAX_WRITE_SIZE As Integer = 512
    Private Shared ReadOnly BLOCK_SIZE As Integer = 16

    Protected _builder As CommandBuilder = Nothing
    Private _adapter As IAdapter = Nothing
    Private _idm As Byte() = Nothing
    Private _tagstatus As Byte = 0
    Private _battery As Byte = 0
    Private _version As Byte = 0

    Private _imageData As AioiSystems.DotModule.ImageInfo = Nothing
    Private _userData As Byte() = Nothing
    Private _function As SmartTagFunctions

    Public Property SelectedFunction() As SmartTagFunctions
        Get
            Return _function
        End Get
        Set
            _function = Value
        End Set
    End Property

    Public Property Adapter As IAdapter
        Get
            Return _adapter
        End Get
        Set(value As IAdapter)
        End Set
    End Property

    Public Sub SetIdm(idm As Byte())
        _idm = idm
    End Sub

    Public Function GetIdm() As Byte()
        Return _idm
    End Function

    Public Sub SetImageData(imageData As ImageInfo)
        _imageData = imageData
    End Sub

    Public Sub SetUserData(data As Byte())
        _userData = data
    End Sub

    Public Function GetUserData() As Byte()
        Return _userData
    End Function

    ''' <summary>
    ''' Indicates whether the smart-tag.
    ''' </summary>
    ''' <param name="idm"></param>
    ''' <returns>True if the IDm is the smart-tag; otherwise, false.</returns>
    Public Shared Function IsSmartTag(idm As Byte()) As Boolean
        If idm Is Nothing Then
            Return False
        End If
        If idm.Length < 8 Then
            Return False
        End If

        Return (idm(0) = CByte(&H3) AndAlso idm(1) = CByte(&HFE) AndAlso idm(2) = CByte(&H0) AndAlso idm(3) = CByte(&H1D))
    End Function

    ''' <summary>
    ''' Starts the smart-tag process.
    ''' </summary>
    Public Sub StartProcess()
        WaitForIdle()
        Select Case Me.SelectedFunction
            Case SmartTagFunctions.ShowImage
                ShowImage(_imageData.GetImage(), 0, 0, _imageData.Width, _imageData.Height, 0, 0)
                Exit Select
            Case SmartTagFunctions.ClearDisplay
                ClearDisplay()
                Exit Select
            Case SmartTagFunctions.WriteUserData
                WriteUserData(0, _userData)
                Exit Select
            Case SmartTagFunctions.ReadUserData
                _userData = ReadUserData(0, 3072)
                SetUserData(_userData)
                WriteToUiLog(Encoding.ASCII.GetString(_userData))
                Exit Select
        End Select

        WaitForIdle()

        If _tagstatus <> TAG_STS_PROCESSED And _tagstatus <> TAG_STS_BUSY Then
            Throw New System.IO.IOException()

        End If

    End Sub

    ''' <summary>
    ''' Waits until smart-tag can do a next task.
    ''' </summary>
    Private Sub WaitForIdle()
        Dim [error] As Exception = Nothing
        For i As Integer = 0 To 600
            Try
                CheckStatus()
            Catch e As Exception
                [error] = e
            End Try
            If _tagstatus <> TAG_STS_BUSY AndAlso _tagstatus <> TAG_STS_UNKNOWN Then
                Return
            End If

            Thread.Sleep(50)
        Next
        If _tagstatus = TAG_STS_UNKNOWN Then
            Throw [error]
        Else
            Throw [error]
        End If
    End Sub

    ''' <summary>
    ''' Confirms the smart-tag status.
    ''' </summary>
    Public Sub CheckStatus()
        _tagstatus = TAG_STS_UNKNOWN
        _version = &H80

        'send request
        Dim paramData As Byte() = New Byte() {0, 0, 0, 0, 0, 0, 0, 0}
        Dim command As Byte() = _builder.BuildCommand(CommandBuilder.COMMAND_CHECK_STATUS, paramData)
        _adapter.Write(_idm, command)

        Thread.Sleep(10)

        'read block
        Dim blockData As Byte() = _adapter.Read(_idm, 2)
        If blockData IsNot Nothing Then
            _tagstatus = blockData(3)
            _battery = blockData(5)
            _version = blockData(15)
            _builder.SetSeq(CByte((blockData(4) + 1)))
        End If
    End Sub

    ''' <summary>
    ''' Sends a command to the smart-tag.
    ''' </summary>
    ''' <param name="command"></param>
    Private Sub SendCommand(command As Byte())
        _adapter.Write(_idm, command)
    End Sub

    ''' <summary>
    ''' Sends a command to the smart-tag.
    ''' </summary>
    ''' <param name="command"></param>
    Private Sub SendCommand(command As Byte(), blocklist As Byte())
        _adapter.Write(_idm, command, blocklist)
    End Sub

    ''' <summary>
    ''' Reads a command from the smart-tag.
    ''' </summary>
    Protected Function ReadData(blocks As Integer) As Byte()
        Return _adapter.Read(_idm, blocks)
    End Function

    ''' <summary>
    ''' Reads a command from the smart-tag.
    ''' </summary>
    Protected Function ReadData(blocks As Integer, blocklist As Byte()) As Byte()
        Return _adapter.Read(_idm, blocks, blocklist)
    End Function

    ''' <summary>
    ''' Displays an image.
    ''' </summary>
    ''' <param name="imageData"></param>
    Private Sub ShowImage(imageData As Byte(), x As Integer, y As Integer, width As Integer, height As Integer, drawMode As Byte,
        layoutNo As Byte)
        Dim list As List(Of Byte()) = Nothing
        Dim paramData As Byte() = Nothing

        Dim pos As Byte() = ConvertTo3Bytes(x, y)
        Dim size As Byte() = ConvertTo3Bytes(width, height)
        Dim mode As Byte = CByte((drawMode << 4))
        mode = mode Or &H3
        paramData = New Byte() {pos(0), pos(1), pos(2), size(0), size(1), size(2),
            layoutNo, mode}
        list = _builder.BuildCommand(CommandBuilder.COMMAND_SHOW_DISPLAY3, paramData, imageData)

        For i As Integer = 0 To list.Count - 1
            If i > 0 Then
                Thread.Sleep(40)
            End If
            SendCommand(list(i))
        Next
    End Sub

    ''' <summary>
    ''' Clears the display.
    ''' </summary>
    Private Sub ClearDisplay()
        Dim paramData As Byte() = New Byte() {0, 0, 0, 0, 0, 0, 0, 0}
        Dim command As Byte() = _builder.BuildCommand(CommandBuilder.COMMAND_CLEAR, paramData)
        SendCommand(command)
    End Sub

    ''' <summary>
    ''' Writes the user data to the free information area on the smart-tag.
    ''' Divides and sends the command into some frames if necessary.
    ''' </summary>
    Private Sub WriteUserData(startAddress As Integer, data As Byte())
        Dim splitCount As Integer = ((data.Length + MAX_WRITE_SIZE - 1) / MAX_WRITE_SIZE) - 1

        Dim offset As Integer = 0
        Dim dataLen As Integer = (If(data.Length <= MAX_WRITE_SIZE, data.Length, MAX_WRITE_SIZE))

        For i As Integer = 0 To splitCount - 1
            If i = splitCount - 1 Then
                'last frame
                dataLen = data.Length - offset
            End If
            Dim framedata As Byte() = New Byte(dataLen - 1) {}
            Array.Copy(data, offset, framedata, 0, dataLen)

            Me.WriteUserDataByFrame(startAddress, framedata)

            offset += dataLen
            startAddress += dataLen

            Thread.Sleep(400)
            WaitForIdle()
        Next
    End Sub

    ''' <summary>
    ''' Writes the user data up to 512-bytes.
    ''' </summary>
    Private Sub WriteUserDataByFrame(address As Integer, data As Byte())
        Dim list As List(Of Byte()) = _builder.BuildDataWriteCommand(address, data)

        For Each cmd As Byte() In list
            SendCommand(cmd)
            Thread.Sleep(40)
        Next
    End Sub

    ''' <summary>
    ''' Reads the user data in the free information area on the smart-tag.
    ''' </summary>
    Public Function ReadUserData(startAddress As Integer, sizeToRead As Integer) As Byte()
        Dim result As Byte() = New Byte(sizeToRead - 1) {}
        Dim maxReadLength As Integer = _builder.getMaxBlocks() * BLOCK_SIZE - BLOCK_SIZE
        Dim splitCount As Integer = (sizeToRead + maxReadLength - 1) / maxReadLength
        Dim dataLen As Integer = (If(sizeToRead > maxReadLength, maxReadLength, sizeToRead))
        Dim offset As Integer = 0

        For i As Integer = 0 To splitCount - 1
            If i = splitCount - 1 Then
                dataLen = sizeToRead - offset
                'last frame
            End If

            Dim data As Byte() = ReadUserDataByBlock(startAddress, dataLen)
            Array.Copy(data, 0, result, offset, dataLen)

            offset += dataLen
            startAddress += dataLen
        Next
        Return result
    End Function

    ''' <summary>
    ''' Reads the user data in the free information area on the smart-tag.
    ''' (Maximum concurrent transfer block number below)
    ''' </summary>
    Private Function ReadUserDataByBlock(readPos As Integer, readSize As Integer) As Byte()
        'Address
        Dim hAByte As Byte = CByte((readPos >> 8))
        Dim lAByte As Byte = CByte((readPos And &HFF))

        'Length
        Dim hLByte As Byte = CByte((readSize >> 8))
        Dim lLByte As Byte = CByte((readSize And &HFF))

        'Sends request for read.
        Dim paramData As Byte() = New Byte() {hAByte, lAByte, hLByte, lLByte, 0, 0, 0, 0}

        Dim command As Byte() = _builder.BuildCommand(CommandBuilder.COMMAND_DATA_READ, paramData)

        SendCommand(command)
        Thread.Sleep(40)

        'Reads data
        Dim blocks As Integer = (readSize + BLOCK_SIZE - 1) / BLOCK_SIZE

        Dim data As Byte() = ReadData(blocks)

        Dim userData As Byte() = New Byte(readSize - 1) {}
        Array.Copy(data, 16, userData, 0, readSize)

        Return userData
    End Function



    ''' <summary>
    ''' Converts two 12-bit numbers to the 3-bytes array.
    ''' </summary>
    Private Shared Function ConvertTo3Bytes(a As Integer, b As Integer) As Byte()
        Dim result As Byte() = New Byte(2) {}
        result(0) = CByte(((a And &HFFF) >> 4))

        Dim wk1 As Byte = CByte(((a And &HF) << 4))
        wk1 = wk1 Or CByte(((b And &HF00) >> 8))
        result(1) = wk1

        result(2) = CByte((b And &HFF))

        Return result
    End Function

    Public Sub WriteToCardArea(blockIndex As Integer, data As Byte())
        Dim maxBlocks As Integer = _builder.getMaxBlocks()
        Dim blocks As Integer = (data.Length + BLOCK_SIZE - 1) / BLOCK_SIZE
        Dim blockData As Byte() = New Byte(blocks * BLOCK_SIZE - 1) {}
        Array.Copy(data, 0, blockData, 0, data.Length)

        Dim frames As Integer = (blocks + maxBlocks - 1) / maxBlocks
        For i As Integer = 0 To frames - 1
            Dim frameBlocks As Integer
            If i = frames - 1 Then
                Dim [mod] As Integer = blocks Mod maxBlocks
                If [mod] = 0 Then
                    frameBlocks = maxBlocks
                Else
                    frameBlocks = [mod]
                End If
            Else
                frameBlocks = maxBlocks
            End If
            Dim command As Byte() = New Byte(frameBlocks * BLOCK_SIZE - 1) {}
            Array.Copy(blockData, i * maxBlocks * 16, command, 0, command.Length)

            Dim blockList As Byte() = New Byte(frameBlocks * 2 - 1) {}
            For j As Integer = 0 To frameBlocks - 1
                blockList(j * 2) = &H80
                blockList(j * 2 + 1) = CByte((j + i * maxBlocks + blockIndex))
            Next
            SendCommand(command, blockList)
        Next
    End Sub

    Public Function ReadFromCardArea(blockIndex As Integer, blocks As Integer) As Byte()
        Dim readData__1 As Byte() = New Byte(blocks * BLOCK_SIZE - 1) {}
        Dim index As Integer = 0
        Dim maxBlocks As Integer = _builder.getMaxBlocks()

        Dim frames As Integer = (blocks + maxBlocks - 1) / maxBlocks
        For i As Integer = 0 To frames - 1
            Dim frameBlocks As Integer
            If i = frames - 1 Then
                Dim [mod] As Integer = blocks Mod maxBlocks
                If [mod] = 0 Then
                    frameBlocks = maxBlocks
                Else
                    frameBlocks = [mod]
                End If
            Else
                frameBlocks = maxBlocks
            End If

            Dim blockList As Byte() = New Byte(frameBlocks * 2 - 1) {}
            For j As Integer = 0 To frameBlocks - 1
                blockList(j * 2) = &H80
                blockList(j * 2 + 1) = CByte((j + i * maxBlocks + blockIndex))
            Next
            Dim data As Byte() = ReadData(frameBlocks, blockList)
            If data IsNot Nothing Then
                Array.Copy(data, 0, readData__1, index, data.Length)
                index += data.Length
            End If
        Next
        Return readData__1
    End Function


End Class