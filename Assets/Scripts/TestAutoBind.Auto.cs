using UnityEngine.UI;
using UnityEngine;
using TMPro;

public partial class TestAutoBind
{
	private AutoBindComponent _AutoBindComponent;
	private AutoBindComponent m_AutoBindComponent
    {
	    get
	    {
		    if (_AutoBindComponent == null)
		    {
			    var comps = gameObject.GetComponents<AutoBindComponent>();
			    foreach (var comp in comps)
			    {
				    if (comp.GeneraterComponent.Equals(this))
				    {
					    _AutoBindComponent = comp;
					    break;
				    }
			    }
		    }
		    return _AutoBindComponent;
	    }
    }
	private Image @Image_Img => (Image)m_AutoBindComponent.m_BindDatas[0].BindComp;
	private RectTransform @Image_RTrans => (RectTransform)m_AutoBindComponent.m_BindDatas[1].BindComp;
	private GameObject @Image_Go => (GameObject)m_AutoBindComponent.m_BindDatas[2].BindComp;
	private GameObject @Text_TMP_Go => (GameObject)m_AutoBindComponent.m_BindDatas[3].BindComp;
	private RectTransform @Text_TMP_RTrans => (RectTransform)m_AutoBindComponent.m_BindDatas[4].BindComp;
	private TextMeshProUGUI @Text_TMP_TextMeshProUGUI => (TextMeshProUGUI)m_AutoBindComponent.m_BindDatas[5].BindComp;
}
