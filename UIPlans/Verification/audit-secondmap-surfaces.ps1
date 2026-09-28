$ErrorActionPreference='Stop'
$path='C:/sf-2D/Assets/00. Member/tyu/SecondMap_remakeCollider.unity'
$text=[IO.File]::ReadAllText($path)
$blocks=$text -split '(?m)(?=^--- !u!)'
$nodes=@{}; $transforms=@{}; $surfaces=@{}; $colliders=@()
foreach($b in $blocks) {
    $h=[regex]::Match($b,'^--- !u!(\d+) &(\d+)'); if(-not $h.Success){continue}
    $id=$h.Groups[2].Value; $type=$h.Groups[1].Value
    $go=[regex]::Match($b,'m_GameObject: \{fileID: (\d+)').Groups[1].Value
    if($type -eq '1') { $nodes[$id]=@{id=$id;name=[regex]::Match($b,'(?m)^  m_Name: (.*)').Groups[1].Value.Trim();layer=[regex]::Match($b,'m_Layer: (\d+)').Groups[1].Value;block=$b;parent=''} }
    if($type -in @('4','224')) { $transforms[$id]=@{go=$go;parent=[regex]::Match($b,'m_Father: \{fileID: (\d+)').Groups[1].Value} }
    if($b -match '09d8eca50718aaf438746e9ba636f418') { $surfaces[$go]=[regex]::Match($b,'material: (\d+)').Groups[1].Value }
    if($type -in @('60','61','64','66','68','70','19719996')) { $colliders+=@{go=$go;type=$type;trigger=($b -match 'm_IsTrigger: 1');block=$b} }
}
foreach($t in $transforms.Values) { if($t.go -and $nodes.ContainsKey($t.go) -and $t.parent -and $transforms.ContainsKey($t.parent)) {$nodes[$t.go].parent=$transforms[$t.parent].go} }
function SurfaceOf($id) {
    $seen=@{}
    while($id -and $id -ne '0' -and -not $seen.ContainsKey($id)) { $seen[$id]=$true; if($surfaces.ContainsKey($id)){return $surfaces[$id]}; $id=$nodes[$id].parent }
    return 'MISSING'
}
function PathOf($id) {
    $parts=@();$seen=@{}
    while($id -and $nodes.ContainsKey($id) -and -not $seen.ContainsKey($id)) {$seen[$id]=$true;$parts=@($nodes[$id].name)+$parts;$id=$nodes[$id].parent}
    return ($parts -join '/')
}
$report=foreach($c in $colliders) {
    $n=$nodes[$c.go]; if(-not $n){continue}
    [PSCustomObject]@{id=$c.go;name=$n.name;path=(PathOf $c.go);parent=$n.parent;layer=$n.layer;type=$c.type;trigger=$c.trigger;surface=(SurfaceOf $c.go)}
}
$report | Export-Csv 'C:/sf-2D/UIPlans/Verification/secondmap-surfaces.csv' -NoTypeInformation
$report | Group-Object surface,layer | Select-Object Count,Name | Format-Table
$report | Where-Object {$_.layer -eq '6' -and -not $_.trigger} | Group-Object {($_.path -split '/')[0]} | Select-Object Count,Name | Format-Table
$surfaces.Keys | ForEach-Object {[PSCustomObject]@{id=$_;path=(PathOf $_);material=$surfaces[$_]}} | Select-Object -First 12 | Format-Table
$nodes.Values | Where-Object {$_.name -eq 'GroundCollider'} | ForEach-Object {[PSCustomObject]@{id=$_.id;path=(PathOf $_.id)}} | Format-Table
