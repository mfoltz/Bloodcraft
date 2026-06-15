param(
    [string]$HostName = "127.0.0.1",
    [int]$Port,
    [string]$Password = "",
    [Parameter(Mandatory = $true)]
    [string]$Command
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function New-RconPacketBytes {
    param(
        [int]$RequestId,
        [int]$Type,
        [string]$Body
    )

    $BodyBytes = [System.Text.Encoding]::UTF8.GetBytes($Body)
    $Size = 4 + 4 + $BodyBytes.Length + 2
    $Buffer = New-Object byte[] (4 + $Size)
    [System.BitConverter]::GetBytes($Size).CopyTo($Buffer, 0)
    [System.BitConverter]::GetBytes($RequestId).CopyTo($Buffer, 4)
    [System.BitConverter]::GetBytes($Type).CopyTo($Buffer, 8)
    [Array]::Copy($BodyBytes, 0, $Buffer, 12, $BodyBytes.Length)
    return $Buffer
}

function Read-ExactBytes {
    param(
        [System.IO.Stream]$Stream,
        [int]$Count
    )

    $Buffer = New-Object byte[] $Count
    $Offset = 0
    while ($Offset -lt $Count) {
        $Read = $Stream.Read($Buffer, $Offset, $Count - $Offset)
        if ($Read -le 0) {
            throw "RCON connection closed while reading packet."
        }

        $Offset += $Read
    }

    return $Buffer
}

function Read-RconPacket {
    param([System.IO.Stream]$Stream)

    $SizeBytes = Read-ExactBytes -Stream $Stream -Count 4
    $Size = [System.BitConverter]::ToInt32($SizeBytes, 0)
    $Payload = Read-ExactBytes -Stream $Stream -Count $Size
    $RequestId = [System.BitConverter]::ToInt32($Payload, 0)
    $Type = [System.BitConverter]::ToInt32($Payload, 4)
    $BodyLength = [Math]::Max(0, $Size - 10)
    $Body = [System.Text.Encoding]::UTF8.GetString($Payload, 8, $BodyLength)

    return [pscustomobject]@{
        RequestId = $RequestId
        Type = $Type
        Body = $Body
    }
}

function Write-RconPacket {
    param(
        [System.IO.Stream]$Stream,
        [int]$RequestId,
        [int]$Type,
        [string]$Body
    )

    $Bytes = New-RconPacketBytes -RequestId $RequestId -Type $Type -Body $Body
    $Stream.Write($Bytes, 0, $Bytes.Length)
    $Stream.Flush()
}

$Client = [System.Net.Sockets.TcpClient]::new()
$Client.Connect($HostName, $Port)
try {
    $Stream = $Client.GetStream()

    Write-RconPacket -Stream $Stream -RequestId 1 -Type 3 -Body $Password
    $Authenticated = $false
    $AuthDeadline = (Get-Date).AddSeconds(10)
    while ((Get-Date) -lt $AuthDeadline -and -not $Authenticated) {
        $Packet = Read-RconPacket -Stream $Stream
        if ($Packet.RequestId -eq 1 -and $Packet.Type -eq 2) {
            $Authenticated = $true
        }
    }

    if (-not $Authenticated) {
        throw "RCON authentication failed."
    }

    Write-RconPacket -Stream $Stream -RequestId 2 -Type 2 -Body $Command
    $Response = Read-RconPacket -Stream $Stream

    [pscustomobject]@{
        Command = $Command
        Response = $Response.Body
        RequestId = $Response.RequestId
        Type = $Response.Type
    } | ConvertTo-Json -Compress
}
finally {
    $Client.Dispose()
}
