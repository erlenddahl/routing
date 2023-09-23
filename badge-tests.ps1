param (
    [Parameter(ValueFromPipeline=$true)]
    [System.IO.FileInfo[]]$trxFiles,
    
    [string]$labelValue,
    [string]$fileValue
)

begin {
    $sumPassed = 0
    $sumTotal = 0
}

process {
    foreach ($trxFile in $trxFiles) {
        $xmlContent = [xml](Get-Content $trxFile.FullName)
        $passedTests = $xmlContent.TestRun.ResultSummary.Counters.passed
        $totalTests = $xmlContent.TestRun.ResultSummary.Counters.total

        $sumPassed += [int]$passedTests
        $sumTotal += [int]$totalTests

        Write-Host "$($trxFile.Directory.Parent.Name): $passedTests / $totalTests"
    }
}

end {
    Write-Host "Total: $sumPassed / $sumTotal"
    $percentage = ($sumPassed / $sumTotal) * 100
    $color = if ($percentage -eq 100) {"green"} elseif ($percentage -gt 90) {"yellow"} else {"red"}
    Remove-Item -Path "$fileValue" -Force
    python -m anybadge --label="$labelValue" --file="$fileValue" --value="$sumPassed / $sumTotal" --color="$color"
}
