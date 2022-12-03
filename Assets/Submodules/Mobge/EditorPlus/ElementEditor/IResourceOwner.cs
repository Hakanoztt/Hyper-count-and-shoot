using Mobge.Core;
using UnityEngine.AddressableAssets;

namespace Mobge
{
	/// <summary>
	/// Overrideable resource owner.
	/// </summary>
	public interface IResourceOwner
	{
		int ResourceCount { get; }
		AssetReference GetResource(int index);
	}
}
