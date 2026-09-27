$ErrorActionPreference='Stop'
$root='C:/sf-2D'
$utf8=[Text.UTF8Encoding]::new($false)
$menuPath=Join-Path $root 'Assets/00. Member/LHS/Scene/MainMenu.unity'
$menu=[IO.File]::ReadAllText($menuPath)
if($menu -match '&8800000001') { throw 'Already connected' }
$guid='8573a1e03f35eb74faf2afcfaf7b4e8d'
$entries=[Text.StringBuilder]::new()
foreach($binding in @(@('5526180716185229460','OnClickStartGame'),@('3643806491432480810','OnClickSettings'),@('1489191934898615569','OnExitGame'))) {
    $fields=@(
        @('m_OnClick.m_PersistentCalls.m_Calls.Array.size','1','0'),
        @('m_OnClick.m_PersistentCalls.m_Calls.Array.data[0].m_Target','','2052880141'),
        @('m_OnClick.m_PersistentCalls.m_Calls.Array.data[0].m_TargetAssemblyTypeName','_00._Member.LHS.Script.MainMenuButtons, Assembly-CSharp','0'),
        @('m_OnClick.m_PersistentCalls.m_Calls.Array.data[0].m_MethodName',$binding[1],'0'),
        @('m_OnClick.m_PersistentCalls.m_Calls.Array.data[0].m_Mode','1','0'),
        @('m_OnClick.m_PersistentCalls.m_Calls.Array.data[0].m_CallState','2','0'))
    foreach($field in $fields) {
        [void]$entries.Append("    - target: {fileID: $($binding[0]), guid: $guid, type: 3}`n      propertyPath: $($field[0])`n      value: $($field[1])`n      objectReference: {fileID: $($field[2])}`n")
    }
}
$menu=$menu.Replace('    m_RemovedComponents:', $entries.ToString()+'    m_RemovedComponents:')
$menu=$menu.Replace('  exitBtn: {fileID: 1644964615}',"  exitBtn: {fileID: 1644964615}`n  menuGroup: {fileID: 8800000001}")
$menu+=@'

--- !u!225 &8800000001 stripped
CanvasGroup:
  m_CorrespondingSourceObject: {fileID: 2112786515164260139, guid: 8573a1e03f35eb74faf2afcfaf7b4e8d, type: 3}
  m_PrefabInstance: {fileID: 1644964614}
  m_PrefabAsset: {fileID: 0}
'@
[IO.File]::WriteAllText($menuPath,$menu,$utf8)

$corePath=Join-Path $root 'Assets/00. Member/LHS/Scene/CoreScene.unity'
$core=[IO.File]::ReadAllText($corePath)
if($core -match '&8800000010') { throw 'Already connected' }
$core=$core.Replace('  settingsPanel: {fileID: 538315665}',"  settingsPanel: {fileID: 538315665}`n  forestSettings: {fileID: 8800000012}")
$transform=[regex]::Match($core,'(?ms)^--- !u!4 &727383556\r?\n.*?(?=^--- !u!|\z)')
if(-not $transform.Success -or $transform.Value -notmatch 'm_Children: \[\]') { throw 'Unexpected UIManager child list' }
$core=$core.Replace($transform.Value,$transform.Value.Replace('  m_Children: []',"  m_Children:`n  - {fileID: 8800000011}"))
$core+=@'

--- !u!1001 &8800000010
PrefabInstance:
  m_ObjectHideFlags: 0
  serializedVersion: 2
  m_Modification:
    serializedVersion: 3
    m_TransformParent: {fileID: 727383556}
    m_Modifications: []
    m_RemovedComponents: []
    m_RemovedGameObjects: []
    m_AddedGameObjects: []
    m_AddedComponents: []
  m_SourcePrefab: {fileID: 100100000, guid: 4b721e41c874fbc4ba13c06c360f30b6, type: 3}
--- !u!224 &8800000011 stripped
RectTransform:
  m_CorrespondingSourceObject: {fileID: 9201127977505514878, guid: 4b721e41c874fbc4ba13c06c360f30b6, type: 3}
  m_PrefabInstance: {fileID: 8800000010}
  m_PrefabAsset: {fileID: 0}
--- !u!114 &8800000012 stripped
MonoBehaviour:
  m_CorrespondingSourceObject: {fileID: 2877825430435629498, guid: 4b721e41c874fbc4ba13c06c360f30b6, type: 3}
  m_PrefabInstance: {fileID: 8800000010}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: fd5f11dae17019048867a58621942c70, type: 3}
  m_Name:
  m_EditorClassIdentifier: Assembly-CSharp::_02._Script.UI.Forest.ForestSettings
'@
[IO.File]::WriteAllText($corePath,$core,$utf8)
Write-Output 'Native menu buttons, foreground CanvasGroup and CoreScene settings prefab connected.'
