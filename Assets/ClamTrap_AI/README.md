# 청회색 조개 함정 애니메이션

닫히면 안쪽 면이 보이지 않도록 완전히 닫힌 외피 원화를 추가했습니다. 0.12초 동안 가속하며 닫히고, 세 개의 짧은 잔상이 뒤따릅니다. 닫힌 뒤 잔상은 사라집니다. 접촉 직후에는 작은 눌림 동작을 넣었습니다.

## Unity에서 미리 보기

1. ClamTrap_Animation.unitypackage를 Unity에서 가져옵니다.
2. Assets/ClamTrap/Demo/ClamTrapPreview.unity를 엽니다.
3. Play를 누르면 열림 → 빠른 닫힘 → 유지 → 다시 열림이 반복됩니다.

씬에 직접 배치할 때는 Assets/ClamTrap/Prefabs/ClamTrap.prefab을 사용합니다. ClamTrapPreview.prefab은 반복 미리보기용입니다.

## 구성

- 두 껍데기 PNG는 별도 오브젝트로 유지했습니다.
- Closed_exterior는 닫힌 순간 안쪽 면을 가리는 추가 원화입니다.
- Ready / Close / Closed / Open 애니메이션 클립과 Animator가 포함됩니다.
- 닫힐 때의 잔상은 ClamTrapPlayer가 SpriteRenderer 복사본으로 표시합니다.
- 패키지는 Unity 기본 Animator와 SpriteRenderer를 사용합니다. Spine 프로젝트나 Spine 런타임 애니메이션 파일은 포함하지 않습니다.

## 함정 연결

ClamTrap 프리팹에는 중앙 발동 영역용 BoxCollider2D가 있습니다. Close On Contact를 켜면 Activation Layers에 해당하는 Collider2D가 들어올 때 닫힙니다. 접촉 대상에는 Unity 2D 물리 규칙에 맞는 Rigidbody2D가 필요합니다. 플레이어 레이어를 Activation Layers에 지정하는 것을 권장합니다.

게임 코드에서 ClamTrapPlayer.Close()로 닫힘, Reopen()으로 열림을 호출할 수도 있습니다. On Snap 이벤트는 닫히는 순간, On Ready 이벤트는 다시 열린 순간 발생합니다. 피해량이나 대상을 판정하는 게임 로직은 포함하지 않았습니다.

Auto Reopen을 끄면 닫힌 채로 유지합니다. 기본값은 0.6초 유지한 뒤 다시 열립니다.

## 원화와 미리보기

- Art/shell_left.png, Art/shell_right.png: 기존 두 껍데기 원화
- Art/closed_exterior.png: 안쪽을 가린 닫힘 원화
- clam_snap_preview.gif: 잔상을 포함한 반복 미리보기
- closed_preview.png: 완전히 닫힌 모습
- afterimage_preview.png: 닫히는 중 잔상을 확인할 수 있는 모습
- motion_settings.json: 움직임 시간과 잔상 설정
- unity_validation.json: 실제 실행한 Unity 검증 결과

GIF는 확인하기 쉬운 중립색 배경을 사용하며, Unity 원화는 투명 배경입니다.

## 원화 편집 방식

내장 imagegen 도구로 닫힘 원화만 한 번 보완했습니다. 나머지 프레임과 잔상은 기존 원화를 움직여 만들었습니다. 도구의 실제 모델 버전은 확인되지 않았습니다.

최종 편집 프롬프트:

Use case: precise-object-edit. Edit the provided closed clam sprite. This is the closed pose of a dark blue-gray fantasy clam trap. Change ONLY the visible pale interior and pale lip seams: hide the interior completely, replacing the entire light gray central opening and white wavy lips with closed opaque outer shell material. The clam must be sealed shut. A single thin dark blue seam between its two valves is acceptable, but absolutely NO white/gray inner lining, no open slit, no pearl, no bright center, no interior visible. Continue the matte hand-painted exterior texture and broad dark desaturated blue-gray shell bands across the former light areas. Dominant palette #264655 with restrained lighter #3B5B68 and darker #192F3B planes. Preserve the existing closed clam's exact tall pointed oval silhouette, size, position, hinge at its bottom, lighting and simple painterly style. Do not change the outer boundary or add new ornaments. Replace the background with a perfectly uniform solid neon green #00FF00 RGB(0,255,0), for later sprite matte extraction. No background texture, no shadow outside the shell, no text. Keep the entire closed clam visible with generous empty margins.

