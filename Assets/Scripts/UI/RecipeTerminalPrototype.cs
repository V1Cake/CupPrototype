using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using CupPrototype.Flair;

namespace CupPrototype.UI
{
    // 只读订单与五条独立示例；浏览配方不写入 Gameplay 或评分数据。
    [DefaultExecutionOrder(-1000)]
    public sealed class RecipeTerminalPrototype : MonoBehaviour
    {
        [Serializable]
        public class Recipe
        {
            public string name, ingredients, method, glass, garnish, taste, style, description, notes;
            public int[] values;
            public Recipe(string n,string i,string m,string g,string garnishText,string t,string s,string desc,string prep,params int[] v)
            { name=n;ingredients=i;method=m;glass=g;garnish=garnishText;taste=t;style=s;description=desc;notes=prep;values=v; }
        }
        [SerializeField] private GameplayUIBridge bridge;
        [SerializeField] private TMP_FontAsset font;
        [SerializeField] private Transform displayCanvas;
        [SerializeField] private GameObject workRoot, browserRoot;
        [SerializeField] private TextMeshProUGUI workTitle, workIngredients, workMethod, workStatus;
        [SerializeField] private Recipe[] recipes = {
            new Recipe("Midnight Bloom","Vodka | 30 ml\nCassis | 15 ml\nLemon | 20 ml\nSyrup | 10 ml","Shake","Coupe","Lemon peel","Sour","Fruity","Dark fruit, bright citrus, a clean finish.","1  Add ingredients and ice to shaker.\n2  Shake until cold.\n3  Strain into a chilled coupe; finish with peel.",72,58,22,66,48,76),
            new Recipe("Amber Circuit","Whiskey | 40 ml\nSweet vermouth | 20 ml\nBitters | 2 ml","Stir","Rocks","Orange peel","Bitter","Spirit","Warm spice and a dry, aromatic finish.","1  Stir ingredients over ice.\n2  Strain over a large cube.\n3  Express orange peel over the glass.",15,40,70,20,82,75),
            new Recipe("Neon Spritz","Gin | 30 ml\nCitrus cordial | 20 ml\nSoda | 60 ml","Build","Highball","Lime wheel","Fresh","Citrus","Light, sparkling citrus with a botanical edge.","1  Fill highball with ice.\n2  Add gin and cordial; top with soda.\n3  Stir gently and garnish.",55,35,20,90,25,60),
            new Recipe("Velvet District","Rum | 35 ml\nVanilla syrup | 15 ml\nCream | 25 ml","Shake","Coupe","Nutmeg","Sweet","Fruity","Soft vanilla and a rich, rounded texture.","1  Add ingredients and ice to shaker.\n2  Shake briefly.\n3  Fine strain; finish with nutmeg.",10,85,12,18,88,65),
            new Recipe("Citrus Signal","Tequila | 35 ml\nLemon | 20 ml\nSyrup | 15 ml","Shake","Rocks","Lemon wheel","Sour","Citrus","A crisp, bright sour with a lively finish.","1  Shake all ingredients with ice.\n2  Strain over fresh ice.\n3  Garnish with a lemon wheel.",85,50,18,72,45,58)
        };
        public bool IsBrowserOpen { get; private set; }
        public string State => IsBrowserOpen ? "RECIPE_BROWSER_STATE" : "WORK_STATE";
        public string Filter { get; private set; } = "A-Z";
        public string FilterValue { get; private set; } = "All";
        public bool DetailExpanded { get; private set; }
        public int SelectedIndex { get; private set; }
        public Recipe SelectedRecipe => recipes[SelectedIndex];
        public string[] VisibleRecipes => Filtered().Select(i=>recipes[i].name).ToArray();
        private readonly Dictionary<Behaviour,bool> inputStates=new Dictionary<Behaviour,bool>();
        private DisplayFocusPrototype legacyFocus;
        private bool legacyFocusEnabled;
        private Canvas legacyDebugCanvas;
        private bool legacyDebugEnabled;
        private GameObject activeCanvas;
        private Transform activeContent;
        private Coroutine returning;
        private CursorLockMode cursorLock;
        private bool cursorVisible;
        private static readonly Color Background=new Color(.025f,.047f,.057f,1);
        private static readonly Color Panel=new Color(.045f,.085f,.102f,1);
        private static readonly Color Accent=new Color(.39f,.87f,.89f,1);

        private void OnEnable()
        {
            if(!Application.isPlaying)return;
            // 原型接管 Tab；相机自身完全不移动。禁用本原型时恢复旧入口。
            // 只隐藏旧 HUD 的绘制，保留它的数据脚本；退出原型时恢复。
            var debugObject=GameObject.Find("DebugCanvas");
            if(debugObject){legacyDebugCanvas=debugObject.GetComponent<Canvas>();if(legacyDebugCanvas){legacyDebugEnabled=legacyDebugCanvas.enabled;legacyDebugCanvas.enabled=false;}}
            legacyFocus=FindAnyObjectByType<DisplayFocusPrototype>();
            if(legacyFocus){legacyFocusEnabled=legacyFocus.enabled;legacyFocus.enabled=false;}
            if(!bridge)bridge=FindAnyObjectByType<GameplayUIBridge>();
            if(bridge)bridge.RecipeChanged+=RefreshWorkState;
            if(!workRoot)BuildPrototype();
            ShowWork();
        }
        private void OnDisable()
        {
            if(!Application.isPlaying)return;
            if(returning!=null)StopCoroutine(returning);
            RestoreInputs();
            if(bridge)bridge.RecipeChanged-=RefreshWorkState;
            if(legacyFocus)legacyFocus.enabled=legacyFocusEnabled;
            if(legacyDebugCanvas)legacyDebugCanvas.enabled=legacyDebugEnabled;
            if(activeCanvas)Destroy(activeCanvas);
            IsBrowserOpen=false;
            if(workRoot)workRoot.SetActive(true);
            if(browserRoot)browserRoot.SetActive(false);
        }
        private void Update()
        {
            if(Input.GetKeyDown(KeyCode.Tab)){if(IsBrowserOpen)ReturnToWork();else OpenBrowser();}
            else if(IsBrowserOpen&&Input.GetKeyDown(KeyCode.Escape))ReturnToWork();
        }
        public void OpenBrowser()
        {
            var interaction = FindAnyObjectByType<CupPrototype.Interaction.InteractionCoordinator>();
            if (interaction && interaction.CurrentActionState != CupPrototype.Interaction.ActionState.Stable) return;
            // 仅在鼠标已释放的空闲帧进入，避免暂停拖拽/倒液的中途状态。
            if(FindAnyObjectByType<CupPrototype.Interaction.MeasurementCameraLock>()?.IsLocked == true)return;
            if(IsBrowserOpen||returning!=null||Input.GetMouseButton(0)||Input.GetMouseButtonUp(0)||Input.GetMouseButton(1)||
                FlairGestureController.IsFlairInputActive||FlairGestureController.IsFlairPlaying||GestureTemplateRecorder.IsTemplateRecording)return;
            inputStates.Clear();
            foreach(var m in FindObjectsByType<MonoBehaviour>())
            {
                string t=m.GetType().Name;
                if(t=="DragController"||t=="DrinkTestManager"||t=="FlairGestureController"||t=="GestureTemplateRecorder"||t=="ShakerController")
                {inputStates[m]=m.enabled;m.enabled=false;}
            }
            cursorLock=Cursor.lockState;cursorVisible=Cursor.visible;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
            IsBrowserOpen=true;DetailExpanded=false;workRoot.SetActive(false);browserRoot.SetActive(true);
            if(!activeCanvas)CreateActiveView();
            activeCanvas.SetActive(true);RenderBrowser();
        }
        public void ReturnToWork()
        {
            if(!IsBrowserOpen)return;
            IsBrowserOpen=false;activeCanvas.SetActive(false);ShowWork();returning=StartCoroutine(RestoreAfterRelease());
        }
        private IEnumerator RestoreAfterRelease()
        {
            // 返回按钮点击不能穿透到台面 Raycast。
            yield return null;
            while(Input.GetMouseButton(0)||Input.GetMouseButton(1))yield return null;
            yield return null;RestoreInputs();returning=null;
        }
        private void RestoreInputs()
        {
            if(inputStates.Count==0)return;
            foreach(var e in inputStates)if(e.Key)e.Key.enabled=e.Value;
            inputStates.Clear();Cursor.lockState=cursorLock;Cursor.visible=cursorVisible;
        }
        private void ShowWork(){workRoot.SetActive(true);browserRoot.SetActive(false);RefreshWorkState();}
        public void RefreshWorkState()
        {
            if(!workTitle)return;
            var order=bridge?bridge.CurrentOrder:null;
            workTitle.text=order?order.drinkName.ToUpperInvariant():"MIDNIGHT BLOOM";
            workIngredients.text=order?string.Join("\n",order.requiredIngredients.Where(i=>i!=null).Select(i=>i.ingredientName)):"Vodka\nCassis\nLemon\nSyrup";
            workMethod.text=order?(order.requiredPreparation==DrinkSystem.TargetDrinkData.PreparationMethod.None?"METHOD NOT SPECIFIED":order.requiredPreparation.ToString().ToUpperInvariant()):"SHAKE / STRAIN";
            workStatus.text=(order?"CURRENT ORDER":"TEST ORDER")+"     |     TAB  RECIPE LIBRARY";
        }
        public void SelectFilter(string v){if(!new[]{"A-Z","Taste","Style","Method"}.Contains(v))return;Filter=v;FilterValue="All";DetailExpanded=false;RenderBrowser();}
        public void SelectOption(string v){if(!Options().Contains(v))return;FilterValue=v;DetailExpanded=false;RenderBrowser();}
        public void SelectRecipe(int i){if(i<0||i>=recipes.Length||!Filtered().Contains(i))return;SelectedIndex=i;DetailExpanded=false;RenderBrowser();}
        public void ToggleDetail(){DetailExpanded=!DetailExpanded;RenderBrowser();}
        private string[] Options()
        {
            switch(Filter){case "Taste":return new[]{"All","Sour","Sweet","Bitter","Fresh"};case "Style":return new[]{"All","Fruity","Citrus","Spirit"};case "Method":return new[]{"All","Shake","Stir","Build"};default:return new[]{"All","A-M","N-Z"};}
        }
        private int[] Filtered()
        {
            return Enumerable.Range(0,recipes.Length).Where(i=>FilterValue=="All"||
                (Filter=="Taste"?recipes[i].taste==FilterValue:Filter=="Style"?recipes[i].style==FilterValue:
                Filter=="Method"?recipes[i].method==FilterValue:FilterValue=="A-M"?recipes[i].name[0]<='M':recipes[i].name[0]>='N'))
                .OrderBy(i=>recipes[i].name,StringComparer.Ordinal).ToArray();
        }
        [ContextMenu("Build P5-2B Prototype")]
        public void BuildPrototype()
        {
            if(!displayCanvas)displayCanvas=GetComponentInParent<Canvas>().transform;
            if(!font)font=displayCanvas.GetComponentInChildren<TextMeshProUGUI>(true).font;
            if(!bridge)bridge=FindAnyObjectByType<GameplayUIBridge>();
            for(int i=transform.childCount-1;i>=0;i--)DestroyImmediate(transform.GetChild(i).gameObject);
            workRoot=Rect("WORK_STATE",transform,0,0,1000,560).gameObject;ImageOn(workRoot,Background);
            workTitle=Label(workRoot.transform,"OrderName","",68,45,28,910,95,Accent);
            workIngredients=Label(workRoot.transform,"KeyIngredients","",46,65,140,870,245,Color.white);
            workMethod=Label(workRoot.transform,"OrderMethod","",44,45,418,910,65,Accent);
            workStatus=Label(workRoot.transform,"OrderStatus","",21,45,512,910,32,Color.gray);
            workTitle.alignment=workIngredients.alignment=workMethod.alignment=workStatus.alignment=TextAlignmentOptions.Center;
            workTitle.enableAutoSizing=true;workTitle.fontSizeMin=46;workTitle.fontSizeMax=68;
            browserRoot=Rect("RECIPE_BROWSER_STATE",transform,0,0,1000,560).gameObject;
            RenderBrowserInto(browserRoot.transform);ShowWork();
        }
        private void CreateActiveView()
        {
            activeCanvas=new GameObject("P5_2B_ActiveView",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            var canvas=activeCanvas.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=300;
            var scaler=activeCanvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            var dim=Rect("InputBackdrop",activeCanvas.transform,0,0,1920,1080);ImageOn(dim.gameObject,new Color(0,0,0,.76f));
            activeContent=Rect("Terminal",activeCanvas.transform,110,64,1000,560);activeContent.localScale=Vector3.one*1.7f;
        }
        private void RenderBrowser()
        {
            var visible=Filtered();if(visible.Length>0&&!visible.Contains(SelectedIndex))SelectedIndex=visible[0];
            if(browserRoot)RenderBrowserInto(browserRoot.transform);
            if(activeContent)RenderBrowserInto(activeContent);
        }
        private void RenderBrowserInto(Transform root)
        {
            // ponytail: 五条原型数据直接重建视图；正式大库再接入列表复用。
            for(int i=root.childCount-1;i>=0;i--){var child=root.GetChild(i).gameObject;child.SetActive(false);if(Application.isPlaying)Destroy(child);else DestroyImmediate(child);}
            ImageOn(root.gameObject,Background);
            Label(root,"Header","RECIPE LIBRARY",29,24,12,540,39,Accent);
            Label(root,"DataScope","5 TEST RECIPES / QUERY ONLY",15,24,50,600,23,Color.gray);
            ButtonAt(root,"ReturnWork","RETURN TO ORDER  [TAB / ESC]",20,690,16,286,48,ReturnToWork,false);
            string[] filters={"A-Z","Taste","Style","Method"};
            for(int i=0;i<filters.Length;i++){string f=filters[i];ButtonAt(root,"Filter_"+f,f.ToUpperInvariant(),22,24+i*158,80,146,39,()=>SelectFilter(f),Filter==f);}
            var options=Options();for(int i=0;i<options.Length;i++){string o=options[i];ButtonAt(root,"Option_"+o,o,17,24+i*125,124,115,29,()=>SelectOption(o),FilterValue==o);}
            var left=Rect("ListPanel",root,24,165,232,277);ImageOn(left.gameObject,Panel);
            Label(left,"ListHeading","RECIPES / "+Filtered().Length,17,12,5,212,26,Accent);
            var viewport=Rect("Viewport",left,6,38,220,231);ImageOn(viewport.gameObject,Panel);viewport.gameObject.AddComponent<RectMask2D>();
            var ids=Filtered();var content=Rect("Content",viewport,0,0,220,Mathf.Max(231,ids.Length*52));
            var scroll=left.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.content=content;scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=25;
            for(int i=0;i<ids.Length;i++){int id=ids[i];ButtonAt(content,"Recipe_"+id,recipes[id].name,20,0,i*52,215,47,()=>SelectRecipe(id),SelectedIndex==id);}
            if(ids.Length==0){Label(root,"Empty","No recipes match this filter.",25,290,210,650,90,Color.white);return;}
            var r=SelectedRecipe;var center=Rect("SelectedRecipe",root,268,165,448,277);ImageOn(center.gameObject,Panel);
            Label(center,"Name",r.name,29,16,8,420,39,Color.white);
            Label(center,"ColumnHeadings","INGREDIENT                          AMOUNT",16,16,53,420,27,Accent);
            string[] rows=r.ingredients.Split('\n');
            for(int i=0;i<rows.Length;i++){var pair=rows[i].Split('|');Label(center,"Ingredient_"+i,pair[0].Trim(),21,16,85+i*29,280,28,Color.white);Label(center,"Amount_"+i,pair.Length>1?pair[1].Trim():"",21,330,85+i*29,100,28,Color.white);}
            Label(center,"MethodGlass","METHOD  "+r.method+"     /     GLASS  "+r.glass,18,16,207,420,28,Accent);
            Label(center,"Garnish","GARNISH  "+r.garnish,18,16,241,420,26,Color.white);
            var right=Rect("TasteStyle",root,728,165,248,277);ImageOn(right.gameObject,Panel);
            Label(right,"Tags",r.taste.ToUpperInvariant()+" / "+r.style.ToUpperInvariant(),19,12,9,228,31,Accent);
            string[] tastes={"Sour","Sweet","Bitter","Fresh","Body","Aroma"};
            for(int i=0;i<6;i++){int x=12+(i%2)*118,y=51+(i/2)*57;Label(right,tastes[i],tastes[i]+"  "+r.values[i],18,x,y,112,27,Color.white);var bar=Rect(tastes[i]+"Track",right,x,y+29,104,5);ImageOn(bar.gameObject,new Color(.12f,.20f,.22f));ImageOn(Rect("Fill",bar,0,0,r.values[i]*1.04f,5).gameObject,Accent);}
            ButtonAt(right,"ExpandDetail",DetailExpanded?"COLLAPSE DETAIL":"EXPAND DETAIL",17,12,233,224,32,ToggleDetail,DetailExpanded);
            Label(root,"Description",r.description,19,24,451,945,27,Color.white);
            Label(root,"PreparationNotes",DetailExpanded?r.notes:"PREPARATION / NOTES   Select EXPAND DETAIL to read the three steps.",DetailExpanded?18:17,24,487,945,65,DetailExpanded?Color.white:Color.gray);
        }
        private static RectTransform Rect(string name,Transform parent,float x,float y,float w,float h)
        {
            var g=new GameObject(name,typeof(RectTransform));g.transform.SetParent(parent,false);var r=g.GetComponent<RectTransform>();
            r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;
        }
        private static void ImageOn(GameObject g,Color color){var image=g.GetComponent<Image>();if(!image)image=g.AddComponent<Image>();image.color=color;}
        private TextMeshProUGUI Label(Transform parent,string name,string text,float size,float x,float y,float w,float h,Color color)
        {
            var t=Rect(name,parent,x,y,w,h).gameObject.AddComponent<TextMeshProUGUI>();t.font=font;t.text=text;t.fontSize=size;t.color=color;t.raycastTarget=false;t.enableWordWrapping=true;t.overflowMode=TextOverflowModes.Ellipsis;return t;
        }
        private void ButtonAt(Transform parent,string name,string text,float size,float x,float y,float w,float h,Action action,bool selected)
        {
            var r=Rect(name,parent,x,y,w,h);ImageOn(r.gameObject,selected?new Color(.11f,.31f,.34f):new Color(.065f,.13f,.155f));
            var b=r.gameObject.AddComponent<Button>();b.targetGraphic=r.GetComponent<Image>();b.onClick.AddListener(()=>action());var nav=b.navigation;nav.mode=Navigation.Mode.None;b.navigation=nav;
            var label=Label(r,"Text",text,size,6,0,w-12,h,selected?Accent:Color.white);label.alignment=TextAlignmentOptions.Midline;
        }
    }
}
