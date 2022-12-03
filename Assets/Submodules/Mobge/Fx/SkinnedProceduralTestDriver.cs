using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mobge.Fx;

public class SkinnedProceduralTestDriver : MonoBehaviour
{
	private SkinnedProcedural sp;
	public Material material;
	public int numberOfNodes;
	private List<float> randomXs;
	private float LerpVal;
	public Sprite sprite;

	void Start()
    {
		sp = gameObject.AddComponent<SkinnedProcedural>();
		sp.Material = material;
		var testPos = new Vector2[numberOfNodes];

		for(int i = 0; i < numberOfNodes; i++) {
			testPos[i] = new Vector2(0, i * 0.8f);
		}
		sp.PieceLength = 0.5f;
		sp.ReConstruct();
		randomXs = new List<float>();
		LerpVal = 0;
		//RegenRandomPoses();
		Debug.Log(sprite);
	}

	private Material GenerateMaterial(Sprite s)
	{
		var m = new Material(Shader.Find("Sprites/Default"));
		m.mainTexture = s.texture;
		return m;
	}


	void LateUpdate()
    {
		//SimulateBandMovement();	
	}

	void RegenRandomPoses()
	{
		randomXs.Clear();
		for (int i = 0; i < sp.ControlTransforms.Length; i++) {
			randomXs.Add(Random.Range(-10.0f, 10.0f));
		}
	}

	void SimulateBandMovement()
	{
		for (int i = 0; i < sp.ControlTransforms.Length; i++) {
			sp.ControlTransforms[i].position = new Vector3(
				Mathf.Lerp(sp.ControlTransforms[i].position.x, randomXs[i], LerpVal),
			 	sp.ControlTransforms[i].position.y,
				sp.ControlTransforms[i].position.z);
		}
		LerpVal += 0.025f;
		if (LerpVal > 1) {
			LerpVal = 0;
			RegenRandomPoses();
		}
	}
}
