using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PawnEquipmentAnimator : MonoBehaviour
{
    public enum EquipmentType
    {
        None,
        Axe,
        Hammer,
        Knife,
        Pickaxe,
        Gold,
        Meat,
        Wood
    }

    [Header("Component")]
    [SerializeField]
    private Animator animator;

    [Header("Animation Controllers")]

    [SerializeField]
    private RuntimeAnimatorController baseController;

    [SerializeField]
    private AnimatorOverrideController axeController;

    [SerializeField]
    private AnimatorOverrideController hammerController;

    [SerializeField]
    private AnimatorOverrideController knifeController;

    [SerializeField]
    private AnimatorOverrideController pickaxeController;

    [SerializeField]
    private AnimatorOverrideController goldController;

    [SerializeField]
    private AnimatorOverrideController meatController;

    [SerializeField]
    private AnimatorOverrideController woodController;

    [Header("Starting Equipment")]

    [SerializeField]
    private EquipmentType startingEquipment =
        EquipmentType.None;

    public EquipmentType CurrentEquipment
    {
        get;
        private set;
    }

 
    public bool CanInteract
    {
        get
        {
            return
                CurrentEquipment == EquipmentType.Axe ||
                CurrentEquipment == EquipmentType.Hammer ||
                CurrentEquipment == EquipmentType.Knife ||
                CurrentEquipment == EquipmentType.Pickaxe;
        }
    }

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        Equip(startingEquipment);
    }


    public void Equip(EquipmentType equipmentType)
    {
        RuntimeAnimatorController selectedController =
            GetController(equipmentType);

        if (selectedController == null)
        {
            Debug.LogWarning(
                "Chưa kéo Controller cho loại: " +
                equipmentType,
                this
            );

            return;
        }

        animator.runtimeAnimatorController =
            selectedController;

        CurrentEquipment =
            equipmentType;
    }

    public void EquipNothing()
    {
        Equip(EquipmentType.None);
    }

    public void EquipAxe()
    {
        Equip(EquipmentType.Axe);
    }

    public void EquipHammer()
    {
        Equip(EquipmentType.Hammer);
    }

    public void EquipKnife()
    {
        Equip(EquipmentType.Knife);
    }

    public void EquipPickaxe()
    {
        Equip(EquipmentType.Pickaxe);
    }

    public void CarryGold()
    {
        Equip(EquipmentType.Gold);
    }

    public void CarryMeat()
    {
        Equip(EquipmentType.Meat);
    }

    public void CarryWood()
    {
        Equip(EquipmentType.Wood);
    }

    private RuntimeAnimatorController GetController(
        EquipmentType equipmentType)
    {
        switch (equipmentType)
        {
            case EquipmentType.None:
                return baseController;

            case EquipmentType.Axe:
                return axeController;

            case EquipmentType.Hammer:
                return hammerController;

            case EquipmentType.Knife:
                return knifeController;

            case EquipmentType.Pickaxe:
                return pickaxeController;

            case EquipmentType.Gold:
                return goldController;

            case EquipmentType.Meat:
                return meatController;

            case EquipmentType.Wood:
                return woodController;

            default:
                return baseController;
        }
    }
}