using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using DG.Tweening;
using Novels;
using Sirenix.OdinInspector;
using Spine.Unity;
using UnityEngine;
using UnityEngine.Playables;

#region Anim的角色控制管理 目前已经弃用

[Serializable]
[Obsolete]//标记该方法已弃用
public class CurrentControlPlayAnim : INovelsSet
{
    
    public string CharaKey;
    public string AnimName;
    public bool IsLoop;
    public float WaitTime;

    public IEnumerator Run() {

        if (StoryScenePrefab.Instance != null && StoryScenePrefab.Instance.CharaMap.ContainsKey(CharaKey)) 
        {
            StoryScenePrefab.Instance.CharaMap[CharaKey].Play(AnimName, IsLoop);
        }
        yield return new WaitForSeconds(WaitTime);
    }
}

[Serializable]
[Obsolete]//标记该方法已弃用
public class PopupClose : INovelsSet
{
    public IEnumerator Run()
    {
        UIPopupWindow.Instance.CloseAll();
        yield break;
    }
}


#endregion

#region TimeLine 3D剧情使用
[Serializable]
public class OverConditionSet : INovelsSet
{ 
    [LabelText("结束检测")]
    public List<INovelsCheck> OverCheck = new List<INovelsCheck>();

    public virtual IEnumerator Run()
    {
        bool isOver = true;
        do
        {
            isOver = true;
            for (int i = 0; i < OverCheck.Count; i++)
            {
                if (!OverCheck[i].Check())
                {
                    isOver = false;
                }
            }

            if (isOver)
            {
                break;
            }

            yield return null;

        } while (!isOver);
    }
}


[Serializable]
public class PlayTimeline : INovelsSet
{
    [LabelText("加载的剧编")]
    public PlayableDirector Playable;
    [LabelText("加载后播放")]
    [ShowIf(("@Playable!=null"))]
    public bool IsAwakePlay=false;

    public OverConditionSet OverCheck;

    public PlayTimeline()
    {
        OverCheck = new OverConditionSet();
        OverCheck.OverCheck.Add(new PlayableOver());
    }

    public IEnumerator Run()
    {
        if (Playable != null)
        {
            if (NovelsManager.Instance.CurrentPlayable != null)
            {
                GameHelper.Recycle(NovelsManager.Instance.CurrentPlayable.gameObject);
            }

            var obj = GameObject.Instantiate(Playable.gameObject);
            NovelsManager.Instance.CurrentPlayable = obj.GetComponent<PlayableDirector>();
        }

        if (Playable == null || IsAwakePlay)
        {
            NovelsManager.Instance.ContineTimeLine();
        }
 
        if (OverCheck != null)
        {
            yield return OverCheck.Run();
        }
    }
}



[LabelText("加载场景")]
[Serializable]
public class LoadScene : INovelsSet
{
    [LabelText("加载的场景")]
    public string SceneId;
 
    public IEnumerator Run()
    {
        yield return UINovelsPanel.Instance.BlackEnter(EBlackType.Black, 0);

        if (AVGManager.Instance.CurrentScene != null)
        {
            GameHelper.Recycle(AVGManager.Instance.CurrentScene.gameObject);
        }

        var obj = GameHelper.Alloc<GameObject>("EventScene/Scene_" + SceneId);
        AVGManager.Instance.CurrentScene = obj.GetComponent<StoryScenePrefab>();
        NovelsManager.Instance.CurrentPlayable = AVGManager.Instance.CurrentScene.PlayableDirector;
    }
}

[LabelText("离开场景")]
[Serializable]
public class LeaveScene : INovelsSet
{
    [LabelText("离场时间")]
    public float FadeTime = 0.5f;

    public IEnumerator Run()
    {
        yield return UINovelsPanel.Instance.BlackEnter(EBlackType.Black, FadeTime);

        if (GameManager.Instance.CurrentScene != null)
        {
            GameHelper.Recycle(AVGManager.Instance.CurrentScene.gameObject);
        }

    }
}

[LabelText("设置当前场景切换舞台")]
[Serializable]
public class SetSceneStage : INovelsSet
{
    public bool IsBlackFade = false;
    [ShowIf("@IsBlackFade")]
    public float FadeTime = 0.5f;

    public ESceneType Stage;

    public IEnumerator Run()
    {
        if (IsBlackFade) 
        {
            yield return UINovelsPanel.Instance.BlackEnter(EBlackType.Black, FadeTime);
        }

        if (SceneSwitchManager.Instance!=null)
        {
            SceneSwitchManager.Instance.SetScene(Stage);
        }

        if (IsBlackFade)
        {
            yield return UINovelsPanel.Instance.BlackLeave(FadeTime);
        }
        yield break;
    }
}

[Serializable]
public class PlayEvent : INovelsSet
{
    [LabelText("加载的剧编")]
    public PlayableAsset Asset;
    [LabelText("加载后播放")]
    [ShowIf(("@Asset!=null"))]
    public bool IsAwakePlay = false;

    public OverConditionSet OverCheck;

    public PlayEvent()
    {
        OverCheck = new OverConditionSet();
        OverCheck.OverCheck.Add(new PlayableOver());
    }

    public IEnumerator Run()
    {
        if (Asset != null)
        {
            AVGManager.Instance.CurrentScene.PlayableDirector.playableAsset = Asset;
        }

        if (Asset == null || IsAwakePlay)
        {
            NovelsManager.Instance.ContineTimeLine();
        }

        if (OverCheck != null)
        {
            yield return OverCheck.Run();
        }
    }
}


[Serializable]
public class ClearTimeLine: INovelsSet
{
    public IEnumerator Run()
    {
        if (NovelsManager.Instance.CurrentPlayable != null)
        {
            GameHelper.Recycle(NovelsManager.Instance.CurrentPlayable.gameObject);
        }
        yield break;
    }
}

#endregion


#region Spine4.1 剧情角色使用

[Serializable]
[LabelText("预加载Spine资源")]
public class 预加载Spine资源: INovelsSet
{
    [LabelText("加载的Spine资源")]
    [ResourcePath(typeof(Spine.Unity.SkeletonDataAsset))]
    public List<String> SpineNodes = new List<string>();
    
    public IEnumerator Run()
    {
        //Todo 黑屏进入
        yield return UINovelsPanel.Instance.BlackEnter(EBlackType.Black, 1f);
        
        foreach (var path in SpineNodes)
        {
            yield return SpineManager.Instance.PreLoadSpine(path);
        }
        
        //黑屏离开
        yield return UINovelsPanel.Instance.BlackLeave( 1f);
    }

}

[Serializable]
[LabelText("Spine角色位置")]
public class 设置Spine角色位置 : INovelsSet
{
    [ResourcePath(typeof(Spine.Unity.SkeletonDataAsset))]
    [LabelText("Spine角色")]
    public string SpineName;
    
    [ValueDropdown("_dropItemList")]
    [LabelText("起始位置")]
    [ShowIf("@SpineName!=null")]
    [InlineButton("FreshItem")]
    [OnValueChanged("OnValueChanged")]
    public string StartPos;

    [ValueDropdown("_dropItemList")]
    [LabelText("最终位置")]
    [ShowIf("@SpineName!=null")]
    [InlineButton("FreshItem")]
    [OnValueChanged("OnValueChanged")]
    public string EndPos;
    
    [LabelText("起始透明度")]
    [Range(0,1)]
    [ShowIf("@SpineName!=null")]
    public float StartAlpha = 0;
    
    [LabelText("最终透明度")]
    [Range(0,1)]
    [ShowIf("@SpineName!=null")]
    public float EndAlpha = 1;

    [LabelText("完成时间")]
    [ShowIf("@SpineName!=null")]
    public float time = 1;
    
    private Vector3 StartPosVec3=Vector3.zero;
    private Vector3 EndPosVec3=Vector3.zero;
    
    IEnumerable<string> _dropItemList;
    
    private Sequence _sequence;
     private void FreshItem()
    {
        _dropItemList = GlobalConfig.Instance.CharaPosList.Select(x => x.PosKey);
    }
    
    private void OnValueChanged()
    {
        StartPosVec3= GlobalConfig.Instance.CharaPosList.Find(x => x.PosKey == StartPos).Pos;
        EndPosVec3= GlobalConfig.Instance.CharaPosList.Find(x => x.PosKey == EndPos).Pos;
        
    }
    
    public IEnumerator Run()
    {
        OnValueChanged();
        //Todo 缓动DT实现，可能角色透明底会分层，后续用RT实现
        var spine = SpineManager.Instance.GetSpine(SpineName);
        spine.transform.localPosition = StartPosVec3;
        SkeletonAnimation skeletonAnimation = spine.GetComponent<SkeletonAnimation>();
        skeletonAnimation.skeleton.A = StartAlpha;
       
        _sequence = DOTween.Sequence();
        _sequence.Append(  DOTween.To(
                () => StartAlpha, //起始值
                x =>
                {
                    skeletonAnimation.skeleton.A = x; //变化值
                },
                EndAlpha, //终点值
                time) //持续时间
            .SetEase(Ease.InCirc) //缓动类型
            .SetUpdate(false)//Time.Scale影响 
            );
        
        _sequence.Insert(0,spine.transform.DOLocalMove(EndPosVec3, time).SetEase(Ease.InCirc).SetUpdate(false));
        _sequence.Play();
      
        
        yield return new WaitForSeconds(time);
        
    }
}

[Serializable]
[LabelText("Spine角色动作")]
public class 播放Spine动画 : INovelsSet
{
    [ResourcePath(typeof(Spine.Unity.SkeletonDataAsset))]
    [OnValueChanged("FreshItem")]
    [LabelText("Spine资源")]
    public string SpineAsset;
    
    [ValueDropdown("_dropSkinList")]
    [LabelText("皮肤名")]
    [ShowIf("@SpineAsset!=null")]
    public string SkinName;
    
    [ValueDropdown("_dropItemList")]
    [LabelText("Spine动画")]
    [ShowIf("@SpineAsset!=null")]
    public string ActionName;
    
    IEnumerable<string> _dropItemList;
    IEnumerable<string> _dropSkinList;
    private void FreshItem()
    {
        SkeletonDataAsset spine = GameHelper.Alloc<SkeletonDataAsset>(SpineAsset);
        
       
        _dropSkinList = spine.GetSkeletonData(true).Skins.Select(x => x.Name);
        _dropItemList = spine.GetSkeletonData(true).Animations.Select(x => x.Name);
        
    }
    
    public IEnumerator Run()
    {
        SpineManager.Instance.PlaySpineAnim(SpineAsset, ActionName,true,SkinName);
        yield return  null;
    }
}


[LabelText("Spine角色登场")]
[Serializable]
[InlineProperty(LabelWidth = 150)]
public class SetRoleEnterStage : INovelsSet
{
    [LabelText("角色入场特效列表")] public List<播放Spine动画> List;

    public IEnumerator Run()
    {
        foreach (var effect in List)
        {
            yield return effect.Run();
        }
    }

}



#endregion