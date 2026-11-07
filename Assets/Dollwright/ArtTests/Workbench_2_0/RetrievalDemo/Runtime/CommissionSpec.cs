using UnityEngine;
namespace Dollwright.RetrievalDemo {
 [CreateAssetMenu(menuName="Dollwright/Retrieval Commission")]
 public class CommissionSpec : ScriptableObject {
  public string brief="铃铛落在笔直的深缝尽头，普通短臂够不到。";
  public Texture2D[] apertureImages;
  public Vector2[] apertures={new Vector2(.48f,1.85f),new Vector2(1.85f,.48f)};
  public float pictureSpan=3.2f;
  public float forwardSpeed=.35f;
 }
}
