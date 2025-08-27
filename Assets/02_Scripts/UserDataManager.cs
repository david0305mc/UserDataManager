using System;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UniRx;
using UnityEngine;

/// <summary>
/// 저장 전용 DTO(직렬화 목적). 런타임 모델과 분리한다.
/// </summary>
[Serializable]
public sealed class CoralDataDto
{
    public int CoralId;
    public int CoralLevel;
}

[Serializable]
public sealed class UserDataDto
{
    public int Version = 1;                    // 스키마 마이그레이션 대비
    public int StoneId;
    public int StoneLevel = 1;
    public Dictionary<int, CoralDataDto> Corals = new Dictionary<int, CoralDataDto>();
    public List<ItemDataDto> Items = new List<ItemDataDto>();
    public Dictionary<int, SkillDataDto> Skills = new Dictionary<int, SkillDataDto>(); 
}

[Serializable]
public class ItemDataDto
{
    public long ItemID;
    public int ItemLevel;
}

[Serializable]
public class SkillDataDto
{
    public long SkillID;
    public int SkillLevel;
}

/// <summary>
/// 런타임 모델(게임 로직에서 사용).
/// 내부 상태는 캡슐화하고, 읽기 전용 인터페이스로만 노출한다.
/// </summary>
/// 
/// 
public class ItemData
{
    public long ItemID;
    public int ItemLevel;
}

public class SkillData
{
    public long SkillID;
    public ReactiveProperty<int> SkillLevel;
}

public sealed class CoralData
{
    public int CoralId { get; private set; }
    public int CoralLevel { get; private set; }

    public CoralData(int coralId, int level)
    {
        CoralId = coralId;
        CoralLevel = Mathf.Max(1, level);
    }

    public void SetLevel(int level) => CoralLevel = Mathf.Max(1, level);
}

[Serializable]
public sealed class UserData
{
    public int StoneId { get; private set; }

    private readonly ReactiveProperty<int> _stoneLevel = new(1);
    public IReadOnlyReactiveProperty<int> StoneLevel => _stoneLevel;

    private readonly Dictionary<int, CoralData> _corals = new();
    public IReadOnlyDictionary<int, CoralData> Corals => _corals;

    private readonly List<ItemData> _items = new();
    public IReadOnlyList<ItemData> Items => _items;

    private readonly Dictionary<int, SkillData> _skills = new();
    public IReadOnlyDictionary<int, SkillData> Skills => _skills;

    public UserData() { }

    public void SetStoneId(int id) => StoneId = id;
    public void SetStoneLevel(int level) => _stoneLevel.Value = Mathf.Max(1, level);

    public bool TryGetCoral(int coralId, out CoralData coral) => _corals.TryGetValue(coralId, out coral);
    public bool TryGetSkill(int skillId, out SkillData skill) => _skills.TryGetValue(skillId, out skill);
    public bool TryGetItem(int itemId, out ItemData item)
    {
        item = _items.Find(i => i.ItemID == itemId);
        return item != null;
    }

    public void UpsertCoral(int coralId, int level)
    {
        if (_corals.TryGetValue(coralId, out var existing))
        {
            existing.SetLevel(level);
        }
        else
        {
            _corals[coralId] = new CoralData(coralId, level);
        }
    }
    public void UpsertItem(int itemId, int level)
    {
        var item = _items.Find(i => i.ItemID == itemId);
        if (item != null)
        {
            item.ItemLevel = Mathf.Max(1, level);
        }
        else
        {
            _items.Add(new ItemData { ItemID = itemId, ItemLevel = Mathf.Max(1, level) });
        }
    }
    public void UpsertSkill(int skillId, int level)
    {
        if (_skills.TryGetValue(skillId, out var existing))
        {
            existing.SkillLevel.Value = Mathf.Max(1, level);
        }
        else
        {
            _skills[skillId] = new SkillData { SkillID = skillId, SkillLevel = new ReactiveProperty<int>(Mathf.Max(1, level))  };
        }        
    }

    public bool RemoveCoral(int coralId) => _corals.Remove(coralId);

    public void InitData()
    {
        InitCorals();
        InitItems();
        InitSkills();
    }
    private void InitCorals()
    {
        _corals.Clear();
        for (int i = 1; i < 10; i++)
        {
            _corals[i] = new CoralData(i, 1);
        }
    }
    private void InitItems()
    {
        _items.Clear();
        for (int i = 1; i < 10; i++)
        {
            _items.Add(new ItemData { ItemID = i, ItemLevel = 1 });
        }
    }
    private void InitSkills()
    {
        _skills.Clear();
        for (int i = 1; i < 10; i++)
        {
            _skills[i] = new SkillData { SkillID = i, SkillLevel =new ReactiveProperty<int>(1) }; 
        }
    }
}


public partial class UserDataManager : Singleton<UserDataManager>, IDisposable
{
    private static readonly string _dirPath = Application.persistentDataPath;
    private static readonly string _fileName = "userdata.json";
    private static readonly string _savePath = Path.Combine(_dirPath, _fileName);

    private readonly Subject<Unit> _saveRequest = new();
    private CompositeDisposable _disposables = new CompositeDisposable();
    private IDisposable _autoSaveSubscription;

    public UserData UserData { get; private set; } = new();

    private static readonly JsonSerializerSettings _jsonSettings = new()
    {
        MissingMemberHandling = MissingMemberHandling.Ignore,
        NullValueHandling = NullValueHandling.Ignore,
        DefaultValueHandling = DefaultValueHandling.Populate,
        Formatting = Formatting.Indented
    };

    /// <summary>
    /// Non-MB 싱글톤 초기화 훅. 여기서 스트림/디바운스 준비를 한다.
    /// </summary>
    protected override void init()
    {
        // 저장 요청이 몰릴 때 500ms 디바운스 후 1회 저장 (메인스레드에서 실행)
        _autoSaveSubscription = _saveRequest
            .ObserveOnMainThread()
            .Throttle(TimeSpan.FromMilliseconds(500))
            .Subscribe(_ => Save());

        _disposables.Add(_autoSaveSubscription);
    }

    public void Dispose()
    {
        _disposables?.Dispose();
        _disposables = new CompositeDisposable(); // 재사용 가능하게 초기화(선택)
    }

    // ====== Public API ======

    /// <summary>디바운스 저장 트리거.</summary>
    public void RequestSave() => _saveRequest.OnNext(Unit.Default);

    /// <summary>즉시 저장(디바운스 미적용). 보통은 RequestSave() 사용.</summary>
    public async UniTask Save()
    {
        var dto = UserData.ToDto();
        await SaveAsync(dto);
    }

    /// <summary>
    /// 저장 파일을 읽어 런타임 모델로 복원하고, 대표 변경 스트림을 자동 저장에 연결한다.
    /// Non-MB이므로 수동으로 Dispose를 호출하거나 앱 종료 때 GC에 맡겨도 무방.
    /// </summary>
    public async UniTask Load()
    {
        var dto = await LoadAsync();
        dto ??= new UserDataDto();
        UserData = dto.FromDto(); 

        // 대표 변경 스트림 → 디바운스 저장
        var sub = UserData.StoneLevel
            .Skip(1)
            .ObserveOnMainThread()
            .Subscribe(_ => RequestSave());

        _disposables.Add(sub);

        Debug.Log("[UserData] Load complete");
    }
    // ====== IO ======

    private static async UniTask SaveAsync(UserDataDto dto)
    {
        try
        {
            if (!Directory.Exists(_dirPath))
                Directory.CreateDirectory(_dirPath);

            var json = JsonConvert.SerializeObject(dto, _jsonSettings);
            var tmpPath = _savePath + ".tmp";

            await File.WriteAllTextAsync(tmpPath, json);

            if (File.Exists(_savePath))
            {
                try
                {
                    File.Replace(tmpPath, _savePath, null);
                }
                catch
                {
                    File.Delete(_savePath);
                    File.Move(tmpPath, _savePath);
                }
            }
            else
            {
                File.Move(tmpPath, _savePath);
            }

            Debug.Log($"[UserData:IO] Saved: {_savePath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[UserData:IO] Save Error: {ex}");
        }
    }

    private static async UniTask<UserDataDto> LoadAsync()
    {
        try
        {
            if (!File.Exists(_savePath))
            {
                Debug.Log("[UserData:IO] No save file. Create new.");
                return new UserDataDto();
            }

            var json = await File.ReadAllTextAsync(_savePath);
            if (string.IsNullOrWhiteSpace(json))
            {
                Debug.LogWarning("[UserData:IO] Empty save file. Create new.");
                return new UserDataDto();
            }

            var dto = JsonConvert.DeserializeObject<UserDataDto>(json, _jsonSettings);
            return dto ?? new UserDataDto();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[UserData:IO] Load Error: {ex}");
            return new UserDataDto();
        }
    }

    // ====== Domain Helpers (변경 + 저장 트리거) ======

    public void SetStone(int stoneId)
    {
        UserData.SetStoneId(stoneId);
        RequestSave();
    }

    public void SetStoneLevel(int level)
    {
        UserData.SetStoneLevel(level);
        RequestSave();
    }

    public void UpsertCoral(int coralId, int level)
    {
        UserData.UpsertCoral(coralId, level);
        RequestSave();
    }

    public bool RemoveCoral(int coralId)
    {
        var removed = UserData.RemoveCoral(coralId);
        if (removed) RequestSave();
        return removed;
    }
    public void UpsertItem(int itemId, int level)
    {
        UserData.UpsertItem(itemId, level);
        RequestSave();
    }
    public void UpsertSkill(int skillId, int level)
    {
        UserData.UpsertSkill(skillId, level);
        RequestSave();
    }
}

public static class UserDataMapper
{
    public static Dictionary<TKey, TDto> MapDict<TKey, TVal, TDto>(
        IReadOnlyDictionary<TKey, TVal> src,
        Func<TVal, TDto> mapper)
    {
        var dst = new Dictionary<TKey, TDto>(src.Count);
        foreach (var kv in src) dst[kv.Key] = mapper(kv.Value);
        return dst;
    }

    public static List<TDto> MapList<TSrc, TDto>(IReadOnlyList<TSrc> src, Func<TSrc, TDto> mapper)
    {
        var list = new List<TDto>(src.Count);
        for (int i = 0; i < src.Count; i++) list.Add(mapper(src[i]));
        return list;
    }
    public static UserDataDto ToDto(this UserData runtime)
    {
        var dto = new UserDataDto
        {
            Version = 1,
            StoneId = runtime.StoneId,
            StoneLevel = runtime.StoneLevel.Value,
            Corals = MapDict(runtime.Corals, ToDto),
            Items = MapList(runtime.Items, ToDto),
            Skills = MapDict(runtime.Skills, ToDto)
        };
        return dto;
    }
    public static UserData FromDto(this UserDataDto dto)
    {
        var runtime = new UserData();
        runtime.InitData();
        runtime.SetStoneId(dto.StoneId);
        runtime.SetStoneLevel(dto.StoneLevel <= 0 ? 1 : dto.StoneLevel);

        if (dto.Corals != null)
        {
            foreach (var kv in dto.Corals)
                runtime.UpsertCoral(kv.Key, Mathf.Max(1, kv.Value.CoralLevel));
        }
        if (dto.Items != null)
        {
            foreach (var item in dto.Items)
                runtime.UpsertItem((int)item.ItemID, Mathf.Max(1, item.ItemLevel));
        }
        if (dto.Skills != null)
        {
            foreach (var skill in dto.Skills)
                runtime.UpsertSkill((int)skill.Value.SkillID, Mathf.Max(1, skill.Value.SkillLevel));
        }
        // 필요시 Version 스위치로 마이그레이션
        return runtime;
    }

    public static CoralDataDto ToDto(this CoralData runtime)
    {
        return new CoralDataDto
        {
            CoralId = runtime.CoralId,
            CoralLevel = runtime.CoralLevel
        };
    }
    public static CoralData FromDto(this CoralDataDto dto)
    {
        return new CoralData(dto.CoralId, dto.CoralLevel);
    }

    public static ItemDataDto ToDto(this ItemData runtime)
    {
        return new ItemDataDto
        {
            ItemID = runtime.ItemID,
            ItemLevel = runtime.ItemLevel
        };
    }
    public static ItemData FromDto(this ItemDataDto dto)
    {
        return new ItemData
        {
            ItemID = dto.ItemID,
            ItemLevel = dto.ItemLevel
        };
    }
    public static SkillDataDto ToDto(this SkillData runtime)
    {
        return new SkillDataDto
        {
            SkillID = runtime.SkillID,
            SkillLevel = runtime.SkillLevel.Value
        };
    }
    public static SkillData FromDto(this SkillDataDto dto)
    {
        return new SkillData
        {
            SkillID = dto.SkillID,
            SkillLevel = new ReactiveProperty<int>(dto.SkillLevel)
        };
    }
}
