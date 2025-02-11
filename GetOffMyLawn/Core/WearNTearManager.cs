namespace GetOffMyLawn;

public static class WearNTearManager {

  public const int HasFieldsHash = -310439593; // HasFields
  public const int HasFieldsWearNTearHash = 2128458598; // HasFieldsWearNTear
  public const int WearNTearNoSupportWearHash = -1943636578; // WearNTear.m_noSupportWear

  public static void SetNoSupportWear(Piece piece) {
    var wear = piece.GetComponent<WearNTear>();
    if (wear) {
      wear.m_noSupportWear = false;
      piece.m_nview.m_zdo.Set(HasFieldsHash, true);
      piece.m_nview.m_zdo.Set(HasFieldsWearNTearHash, true);
      piece.m_nview.m_zdo.Set(WearNTearNoSupportWearHash, false);
    }
  }
}
