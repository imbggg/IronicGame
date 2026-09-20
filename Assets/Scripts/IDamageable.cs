/// <summary>
/// 플레이어 공격이 때릴 수 있는 대상. Monster와 Boss가 함께 구현한다.
/// 새 적을 추가할 때 이것만 구현하면 공격 코드는 손대지 않아도 된다.
/// </summary>
public interface IDamageable
{
    void TakeDamage(int damage);
}
