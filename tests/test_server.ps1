# GStage Automated Verification Test Script
# Tests all core features, RFC numerics, and authentic Quarterdeck exploits

$ErrorActionPreference = "Stop"

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host " Running GStage Verification Test Suite   " -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan

$testPort = 6699
$serverProcess = Start-Process -FilePath "bin\GStage.exe" -ArgumentList "-p $testPort -pass secretpass" -PassThru -NoNewWindow
Start-Sleep -Milliseconds 800

function Read-Lines($stream, $timeoutMs = 2000) {
    $buffer = New-Object byte[] 4096
    $lines = New-Object System.Collections.Generic.List[string]
    $deadline = (Get-Date).AddMilliseconds($timeoutMs)
    $sb = New-Object System.Text.StringBuilder
    
    while ((Get-Date) -lt $deadline) {
        if ($stream.DataAvailable) {
            $read = $stream.Read($buffer, 0, $buffer.Length)
            if ($read -gt 0) {
                $text = [System.Text.Encoding]::UTF8.GetString($buffer, 0, $read)
                $sb.Append($text)
                $all = $sb.ToString()
                $idx = $all.IndexOf("`n")
                while ($idx -ge 0) {
                    $line = $all.Substring(0, $idx).Trim("`r", "`n")
                    $lines.Add($line)
                    $all = $all.Substring($idx + 1)
                    $idx = $all.IndexOf("`n")
                }
                $sb.Clear()
                $sb.Append($all)
            }
        } else {
            Start-Sleep -Milliseconds 50
        }
    }
    return $lines
}

function Send-Line($stream, $line) {
    $bytes = [System.Text.Encoding]::UTF8.GetBytes("$line`r`n")
    $stream.Write($bytes, 0, $bytes.Length)
    $stream.Flush()
}

$passed = 0
$failed = 0

function Assert-Test($condition, $description) {
    if ($condition) {
        Write-Host "  [PASS] $description" -ForegroundColor Green
        $script:passed++
    } else {
        Write-Host "  [FAIL] $description" -ForegroundColor Red
        $script:failed++
    }
}

try {
    # 1. Connect Client 1 (Bob)
    $client1 = New-Object System.Net.Sockets.TcpClient("127.0.0.1", $testPort)
    $stream1 = $client1.GetStream()
    Send-Line $stream1 "NICK Bob"
    Send-Line $stream1 "USER bob localhost GStage :Bob The Builder"
    
    $c1Lines = Read-Lines $stream1 1200
    
    # Verify RPL_WELCOME (001)
    $hasWelcome = $c1Lines | Where-Object { $_ -match "001 Bob :Welcome to the Internet Relay Network, Bob" }
    Assert-Test ($null -ne $hasWelcome) "RPL_WELCOME (001) received with correct nickname"
    
    # Verify RPL_YOURHOST (002) with glorious steam
    $hasSteam = $c1Lines | Where-Object { $_ -match "running on glorious steam" }
    Assert-Test ($null -ne $hasSteam) "RPL_YOURHOST (002) contains Quarterdeck 'glorious steam' signature"
    
    # Verify MOTD with Dominion art
    $hasDominion = $c1Lines | Where-Object { $_ -match "WELCOME TO THE DOMINION" }
    Assert-Test ($null -ne $hasDominion) "SciFi Channel #dominion MOTD banner received"

    # 2. Test JOIN #dominion
    Send-Line $stream1 "JOIN #dominion"
    $joinLines = Read-Lines $stream1 800
    $hasJoin = $joinLines | Where-Object { $_ -match "JOIN #dominion" }
    Assert-Test ($null -ne $hasJoin) "Client received JOIN #dominion acknowledgment"
    
    # Check that first user is made Op (@Bob)
    $hasOp = $joinLines | Where-Object { $_ -match "= #dominion :@Bob" }
    Assert-Test ($null -ne $hasOp) "First user in channel automatically granted operator status (@Bob)"

    # 3. Connect Client 2 (Alice)
    $client2 = New-Object System.Net.Sockets.TcpClient("127.0.0.1", $testPort)
    $stream2 = $client2.GetStream()
    Send-Line $stream2 "NICK Alice"
    Send-Line $stream2 "USER alice localhost GStage :Alice Wonderland"
    $c2Lines = Read-Lines $stream2 800

    # Test Global Stage Hostmask format (.this.chat.server)
    $whoisLine = $null
    Send-Line $stream2 "WHOIS Bob"
    $whoisLines = Read-Lines $stream2 800
    $hasHostmask = $whoisLines | Where-Object { $_ -match "this\.chat\.server" }
    Assert-Test ($null -ne $hasHostmask) "Authentic 4-tier hostmask generated (*.this.chat.server)"

    # 4. Client 2 joins #dominion
    Send-Line $stream2 "JOIN #dominion"
    $c2JoinLines = Read-Lines $stream2 800
    $c1Broadcast = Read-Lines $stream1 500
    $c1HeardJoin = $c1Broadcast | Where-Object { $_ -match ":Alice!.*JOIN #dominion" }
    Assert-Test ($null -ne $c1HeardJoin) "Client 1 received join broadcast from Client 2"

    # 5. Test PRIVMSG
    Send-Line $stream2 "PRIVMSG #dominion :Greetings SciFi fans!"
    $c1Msg = Read-Lines $stream1 500
    $c1GotMsg = $c1Msg | Where-Object { $_ -match "PRIVMSG #dominion :Greetings SciFi fans!" }
    Assert-Test ($null -ne $c1GotMsg) "Channel message delivered correctly"

    # 6. Test Quarterdeck Nickname Change Prohibition
    # Normal registered users CANNOT change nickname!
    Send-Line $stream2 "NICK NewAlice"
    $nickErrLines = Read-Lines $stream2 500
    $hasNickErr = $nickErrLines | Where-Object { $_ -match "432 Alice NewAlice :Erroneous nickname" }
    Assert-Test ($null -ne $hasNickErr) "Normal users cannot change nick once registered (432 ERR_ERRONEOUSNICKNAME)"

    # 7. Test Supervisor elevation and nick change permission
    Send-Line $stream1 "MODE Bob +s secretpass"
    $superLines = Read-Lines $stream1 500
    $hasSuperMode = $superLines | Where-Object { $_ -match "MODE Bob :\+s" }
    Assert-Test ($null -ne $hasSuperMode) "Supervisor authentication succeeded (MODE +s)"

    # Supervisor CAN change nickname
    Send-Line $stream1 "NICK CommanderBob"
    $superNickLines = Read-Lines $stream1 500
    $hasSuperNick = $superNickLines | Where-Object { $_ -match "NICK CommanderBob" }
    Assert-Test ($null -ne $hasSuperNick) "Supervisor permitted to change nickname"

    # 8. Test Quarterdeck '-k' OP EXPLOIT!
    # Bob (op) sets channel key
    Send-Line $stream1 "MODE #dominion +k opensesame"
    Start-Sleep -Milliseconds 200

    # Alice (regular user) sends MODE #dominion -k opensesame
    Send-Line $stream2 "MODE #dominion -k opensesame"
    $exploitLines = Read-Lines $stream2 800
    $hasExploitOp = $exploitLines | Where-Object { $_ -match "MODE #dominion :\+o Alice" }
    Assert-Test ($null -ne $hasExploitOp) "Famous Global Stage exploit verified: 'MODE -k <key>' grants OP!"

    # 9. Test KICK and 180-second Bozo IP ban
    Send-Line $stream1 "KICK #dominion Alice :Out you go"
    $kickLines = Read-Lines $stream2 500
    $hasKicked = $kickLines | Where-Object { $_ -match "KICK #dominion Alice :Out you go" }
    Assert-Test ($null -ne $hasKicked) "User kicked from channel"

    # Alice tries to rejoin immediately -> should be blocked by Bozo filter (474)
    Send-Line $stream2 "JOIN #dominion"
    $banLines = Read-Lines $stream2 500
    $hasBanned = $banLines | Where-Object { $_ -match "474 Alice #dominion :Cannot join channel \(\+b\)" }
    Assert-Test ($null -ne $hasBanned) "Kicked user automatically placed on Bozo ban list (474 ERR_BANNEDFROMCHAN)"

    # Clean up clients
    Send-Line $stream1 "QUIT :Bye"
    Send-Line $stream2 "QUIT :Bye"
    $client1.Close()
    $client2.Close()

} finally {
    if ($serverProcess -and !$serverProcess.HasExited) {
        Stop-Process -Id $serverProcess.Id -Force
    }
}

Write-Host "==========================================" -ForegroundColor Cyan
$resultColor = if ($failed -eq 0) { "Green" } else { "Red" }
Write-Host " Results: $passed Passed, $failed Failed" -ForegroundColor $resultColor
Write-Host "==========================================" -ForegroundColor Cyan

if ($failed -gt 0) {
    exit 1
}
exit 0
