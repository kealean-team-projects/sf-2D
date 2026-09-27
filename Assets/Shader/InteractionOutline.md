# 상호작용 외곽선

`InteractableInnerOutline.shader`와 `Resources/Interaction/InnerOutline.mat`을 사용합니다.
기존 빈 `InteracableOutline.shadergraph`는 보존했으며, 실행에는 사용하지 않습니다.

- `Light`, `TestInteract`는 `InteracterBase`를 상속합니다.
- 오브젝트 또는 자식에 `SpriteRenderer`와 스프라이트가 있으면 자동으로 찾습니다.
- 렌더러가 여러 개라면 컴포넌트의 `Target Renderer`에 강조할 하나를 지정합니다.
- `Collider2D`는 상호작용 스크립트와 같은 오브젝트에 있어야 합니다.
- 플레이어 `Interactor`의 Radius와 Target 필터에 들어오는 가장 가까운 대상 하나가 강조됩니다.
- 기본 두께는 원본 텍스처의 1픽셀입니다. 이미지 크기 입력이나 투명 여백 추가는 필요 없습니다.
- 대상에서 벗어나거나 컴포넌트를 끄면 원래 머티리얼과 프로퍼티 블록을 복원합니다.
- 별도 조건은 `CanInteract(Player owner)`를 재정의합니다.

외곽선 머티리얼은 Unlit이므로 강조 중에는 스프라이트가 2D 조명의 영향을 받지 않습니다.
SpriteRenderer의 Simple 모드, 일반 스프라이트 및 사각형으로 패킹된 아틀라스 영역을 대상으로 합니다.
Tight Packing으로 다른 그림이 UV 경계 사각형 안에 섞이는 아틀라스는 지원 대상이 아닙니다.
키 안내 UI와 조명 켜기/끄기 동작은 이 외곽선 작업에 포함하지 않습니다.
