using UnityEngine;
using Mobge.Fx;

public class ElasticBandGraphics : MonoBehaviour
{
	public AElasticBandData Data { get; set; }
	public SkinnedProcedural _proceduralSkin;
    public SkinnedProcedural Skin {
        get {
            EnsureSkin();
            return _proceduralSkin;
        }
    }

	private void EnsureSkin() {
        if (_proceduralSkin == null) {
            _proceduralSkin = GetComponent<SkinnedProcedural>();
            if (_proceduralSkin == null) {
                _proceduralSkin = gameObject.AddComponent<SkinnedProcedural>();
            }
        }
    }
    public void ReConstruct(Material material) {
		Skin.Material = material;
        ReConstruct();
    }

    public void ReConstruct() {
		_proceduralSkin.ReConstruct();
	}

	public void Paint()
	{
		Paint(Data);
	}

	private Vector3 GetDirection(Vector3 pos1, Vector3 pos2)
	{
		var dif = (pos2 - pos1);
		var mag = dif.magnitude;
		return dif / mag;
	}

	private void Paint(AElasticBandData data)
	{
		int length = _proceduralSkin.ControlTransforms.Length-1;
		_proceduralSkin.ControlTransforms[length].position = data.StartPoint;
		Vector3 dir = GetDirection(_proceduralSkin.ControlTransforms[0].position, _proceduralSkin.ControlTransforms[length - 1].position);
		_proceduralSkin.ControlTransforms[length].localRotation = Quaternion.LookRotation(Vector3.forward, dir);
		dir = GetDirection(_proceduralSkin.ControlTransforms[0].position, _proceduralSkin.ControlTransforms[1].position);
		_proceduralSkin.ControlTransforms[0].localRotation = Quaternion.LookRotation(Vector3.forward, dir);
		for (int i = 0; i < length; i++) {
			if (i != 0 || i != length - 1) {
				_proceduralSkin.ControlTransforms[i].position = data.Points[i];
			}
			dir = GetDirection(_proceduralSkin.ControlTransforms[i].position, _proceduralSkin.ControlTransforms[i + 1].position);
			_proceduralSkin.ControlTransforms[i].localRotation = Quaternion.LookRotation(Vector3.forward, dir);
		}
	}
	public void SnapBack(bool isToAnchor)
	{
		if (isToAnchor) {
			for (int i = 0; i < _proceduralSkin.ControlTransforms.Length; i++)
				_proceduralSkin.ControlTransforms[i].position = Data.StartPoint;
		} else {
			for (int i = 0; i < _proceduralSkin.ControlTransforms.Length; i++)
				_proceduralSkin.ControlTransforms[i].position = Data.EndPoint;
		}
	}
}
