using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static scr_Models;

public class scr_WeaponController : MonoBehaviour
{
    private scrCharacterControl characterController;

    [Header("References")]
    public Animator weaponAnimator;
    public GameObject bulletPrefab;
    public Transform bulletSpawn;

    [Header("Settings")]
    public WeaponSettingsModel settings;

    bool isInitialised;

    Vector3 newWeaponRotation;
    Vector3 newWeaponRotationVelocity;

    Vector3 targetWeaponRotation;
    Vector3 targetWeaponRotationVelocity;

    Vector3 newWeaponMovementRotation;
    Vector3 newWeaponMovementRotationVelocity;

    Vector3 targetWeaponMovementRotation;
    Vector3 targetWeaponMovementRotationVelocity;

    private bool isGroundedTrigger;

    private float FallingDelay;
    
    [Header("Weapon Sway")]
    public Transform weaponSwayObject;
    public float sswayAmountA = 1;
    public float swayAmountB = 2;
    public float swayScale = 400;
    public float swayLerpspeed = 14;

    float swayTime;
    Vector3 breathingPosition;

    [Header("Sights")]
    public Transform sightTarget;
    public float sightOffset;
    public float aimingInTime;
    private Vector3 weaponSwayPosition;
    private Vector3 weaponSwayPositionVelocity;
    [HideInInspector]
    public bool isAimingIn;

    [Header("Shooting")]
    public float rateOfFire;
    private float currentFireRate;
    public List<WeaponFireType> allowedFireTypes;
    public WeaponFireType currentFireType;
    public bool isShooting;
    public ParticleSystem MuzzleFlash;
    [HideInInspector]
    //public bool isShooting;
    public float damage = 10f;
    public float range = 100f;
    //public ParticleSystem MuzzleFlash;
    public GameObject impactEffect;
    public float ImpactForce = 30f;
    public float FireRate = 15f;
    private float nextTimeToFire = 0f;

    #region - Awake / Start / Update -

    private void Start(){
        newWeaponRotation = transform.localRotation.eulerAngles;
        currentFireType = allowedFireTypes.First();
    }

    private void Update(){
        if(!isInitialised){
            return;
        }

        CalculateWeaponRotation();
        SetWeaponAnimations();
        CalculateWeaponSway();
        CalculateAimingIn();
        //CalculateShooting();
        Shooting();
        
    }

    #endregion

    #region - Shooting -

    private void CalculateShooting(){
        if (isShooting){
            Shoot();
            MuzzleFlash.Play();
            if (currentFireType == WeaponFireType.SemiAuto){
                isShooting = false;
            }
        }
    }

    private void Shoot(){
        var bullet = Instantiate(bulletPrefab, bulletSpawn);

        //Load bullet settings
    }

    private void Shooting(){
        if(isShooting && Time.time >= nextTimeToFire){
            nextTimeToFire = Time.time + 1f/FireRate;
            MuzzleFlash.Play();
            RaycastHit hit;
            if (Physics.Raycast(sightTarget.transform.position, sightTarget.transform.forward, out hit, range)){
                Debug.Log(hit.transform.name);
                Target target = hit.transform.GetComponent<Target>();

                if (target != null){
                    target.TakeDamage(damage);
                }

                if (hit.rigidbody != null){
                    hit.rigidbody.AddForce(-hit.normal * ImpactForce);
                }
                
                GameObject impactGO = Instantiate(impactEffect, hit.point, Quaternion.LookRotation(hit.normal));
                Destroy(impactGO, 2f);
            }
        }
    }

    #endregion

    #region - initialise -
    public void Initilise(scrCharacterControl characterControl){
        characterController = characterControl;
        isInitialised = true;
    }
    #endregion

    #region - Aiming in -
    private void CalculateAimingIn(){
        var targetPosition = transform.position;

        if (isAimingIn && !characterController.isSprinting){
            targetPosition = characterController.camera_.transform.position + (weaponSwayObject.transform.position - sightTarget.position) + (characterController.camera_.transform.forward * sightOffset);
        }

        weaponSwayPosition = weaponSwayObject.transform.position;
        weaponSwayPosition = Vector3.SmoothDamp(weaponSwayPosition, targetPosition, ref weaponSwayPositionVelocity, aimingInTime);
        weaponSwayObject.transform.position = weaponSwayPosition + breathingPosition;

    }

    #endregion

    #region - Jumping -
    public void TriggerJump(){
        isGroundedTrigger = false;
        weaponAnimator.SetTrigger("Jump");
    }
    
    #endregion

    #region - Rotation -
    private void CalculateWeaponRotation(){
        

        targetWeaponRotation.y += (isAimingIn ? settings.SwayAmount / 3: settings.SwayAmount) * (settings.SwayXInverted ? -characterController.input_View.x : characterController.input_View.x) * Time.deltaTime;
        targetWeaponRotation.x += (isAimingIn ? settings.SwayAmount / 3: settings.SwayAmount) * (settings.SwayYInverted ? characterController.input_View.y : -characterController.input_View.y) * Time.deltaTime;
        
        targetWeaponRotation.x = Mathf.Clamp(targetWeaponRotation.x, -settings.SwayClampX, settings.SwayClampX);
        targetWeaponRotation.y = Mathf.Clamp(targetWeaponRotation.y, -settings.SwayClampY, settings.SwayClampY);
        targetWeaponRotation.z = isAimingIn ? 0 : targetWeaponRotation.y;

        targetWeaponRotation = Vector3.SmoothDamp(targetWeaponRotation, Vector3.zero, ref targetWeaponRotationVelocity, settings.SwayResetSmoothing);
        newWeaponRotation = Vector3.SmoothDamp(newWeaponRotation, targetWeaponRotation, ref newWeaponRotationVelocity, settings.SwaySmoothing);

        targetWeaponMovementRotation.z = (isAimingIn ? settings.MovementSwayX / 3 : settings.MovementSwayX) * (settings.MovementSwayXInverted ? -characterController.input_Movement.x : characterController.input_Movement.x);
        targetWeaponMovementRotation.x = (isAimingIn ? settings.MovementSwayY / 3 : settings.MovementSwayY) * (settings.MovementSwayYInverted ? -characterController.input_Movement.y : characterController.input_Movement.y);

        targetWeaponMovementRotation = Vector3.SmoothDamp(targetWeaponMovementRotation, Vector3.zero, ref targetWeaponMovementRotationVelocity, settings.MovementSwaySmoothing);
        newWeaponMovementRotation = Vector3.SmoothDamp(newWeaponMovementRotation, targetWeaponMovementRotation, ref newWeaponMovementRotationVelocity, settings.MovementSwaySmoothing);

        transform.localRotation = Quaternion.Euler(newWeaponRotation + newWeaponMovementRotation);
    }

    #endregion

    #region - Animations -
    private void SetWeaponAnimations(){
        if (isGroundedTrigger){
            FallingDelay = 0;
        }
        else{
            FallingDelay += Time.deltaTime;
        }

        if (characterController.isGrounded && !isGroundedTrigger && FallingDelay > 0.1f){
           
            weaponAnimator.SetTrigger("Land");
            isGroundedTrigger = true;
        }

        if (!characterController.isGrounded && isGroundedTrigger){
            
            weaponAnimator.SetTrigger("Falling");
            isGroundedTrigger = false;
        }

        weaponAnimator.SetBool("IsSprinting", characterController.isSprinting);
        weaponAnimator.SetBool("IsShooting", isShooting);
        weaponAnimator.SetFloat("WeaponAnimationSpeed", characterController.weaponAnimationSpeed);
    }

    #endregion

    #region - sway -
    private void CalculateWeaponSway(){
        var targetPositon = LissajousCurve(swayTime, sswayAmountA, swayAmountB) / (isAimingIn ? swayScale * 3 : swayScale);
        
        breathingPosition = Vector3.Lerp(breathingPosition, targetPositon, Time.smoothDeltaTime * swayLerpspeed);
        swayTime += Time.deltaTime;

        if (swayTime > 6.3f){
            swayTime = 0;
        }

        
    }

    private Vector3 LissajousCurve(float Time, float A, float B){
        return new Vector3(Mathf.Sin(Time), A * Mathf.Sin(B * Time + Mathf.PI));
    }

    #endregion
    
}
