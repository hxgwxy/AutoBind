using UnityEngine.UI;
using UnityEngine;
using TMPro;

public partial class TestAutoBind
{
	private AutoBindComponent _AutoBindComponent;
	private AutoBindComponent m_AutoBindComponent => _AutoBindComponent ??= gameObject.GetComponent<AutoBindComponent>();
	private Image Image_Img => (Image)m_AutoBindComponent.m_BindDatas[0].BindComp;
	private RectTransform Image_RTrans => (RectTransform)m_AutoBindComponent.m_BindDatas[1].BindComp;
	private TextMeshProUGUI Text_TMP_TextMeshProUGUI => (TextMeshProUGUI)m_AutoBindComponent.m_BindDatas[2].BindComp;
	private RectTransform Text_TMP_RTrans => (RectTransform)m_AutoBindComponent.m_BindDatas[3].BindComp;
	private GameObject Text_TMP_Go => (GameObject)m_AutoBindComponent.m_BindDatas[4].BindComp;
	private RectTransform Scroll_View_RTrans => (RectTransform)m_AutoBindComponent.m_BindDatas[5].BindComp;
	private Image Scroll_View_Img => (Image)m_AutoBindComponent.m_BindDatas[6].BindComp;
	private ScrollRect Scroll_View_ScrollRect => (ScrollRect)m_AutoBindComponent.m_BindDatas[7].BindComp;
}
