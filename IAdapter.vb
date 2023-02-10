Public Interface IAdapter
    Property Context() As Integer

    Property Name() As String

    Function Poll() As Boolean

    Function GetIdm() As Byte()

    Sub OpenCard()

    Function CloseCard() As Integer

    Sub OpenAdapter()

    Function CloseAdapter() As Integer

    Sub Write(idm As Byte(), command As Byte())

    Sub Write(idm As Byte(), command As Byte(), blocklist As Byte())

    Function Read(idm As Byte(), blocks As Integer) As Byte()

    Function Read(idm As Byte(), blocks As Integer, blocklist As Byte()) As Byte()
End Interface
