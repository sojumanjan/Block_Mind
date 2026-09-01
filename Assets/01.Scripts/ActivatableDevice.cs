using UnityEngine;

// ButtonZone처럼 무언가를 켜고 끄는 장치가 연결 대상으로 삼는 공통 타입.
//
// 인터페이스가 아니라 추상 MonoBehaviour인 이유:
// Unity는 인터페이스 필드를 인스펙터에 직렬화하지 못한다. MonoBehaviour[]로 받고 캐스팅하면
// 아무 컴포넌트나 끌어다 놓을 수 있게 되어 타입 안정성을 잃는다.
// 추상 클래스로 두면 인스펙터가 이 타입만 받아준다.
//
// 새 장치를 만들 때 이걸 상속하면 ButtonZone 쪽은 손대지 않아도 된다.
public abstract class ActivatableDevice : MonoBehaviour
{
    public abstract void SetActivated(bool activated);
}
