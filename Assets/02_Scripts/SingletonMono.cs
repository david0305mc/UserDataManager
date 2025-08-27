using UnityEngine;

/// <summary>
/// 씬 간에 지속적인 데이터를 관리하기 위한 싱글톤 패턴입니다.
/// </summary>
public abstract class SingletonMono<T> : MonoBehaviour where T : MonoBehaviour
{
	private static T instance;

	public static T Instance
	{
		get
		{
			if (instance == null)
			{
				instance = Object.FindFirstObjectByType<T>();
				if (instance == null)
				{
					GameObject go = new GameObject(typeof(T).Name);
					instance = go.AddComponent<T>();
				}
			}
			return instance;
		}
	}

	[SerializeField] private bool dontDestroyOnLoad = true;

	protected virtual void Awake()
	{
		if (instance == null)
		{
			instance = this as T;

			if (dontDestroyOnLoad && transform.parent == null)
			{
				DontDestroyOnLoad(gameObject);
			}

			OnInitialize();
		}
		else if (instance != this)
		{
			DestroyImmediate(gameObject);
		}
	}

	protected virtual void OnDestroy()
	{
		if (instance == this)
		{
			instance = null;
		}
	}

	/// <summary>
	/// 싱글톤 초기화 직후 호출되는 메서드입니다.
	/// 초기 설정을 위해 오버라이드하여 사용하세요.
	/// </summary>
	protected virtual void OnInitialize()
	{
	}
}