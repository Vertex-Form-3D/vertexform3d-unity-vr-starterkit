# Avatar Creator Elements

As part of the Unity SDK 5.0.0 update we added a new set of classes we call Avatar Creator elements. 
These elements are designed to be simple and modular and can be used as simplified building blocks when it comes to
creating your own Avatar Creator UI. We tried to keep them as simple as possible so that the code in each script would
be relatively simple to understand, which will hopefully make it easier for you to create your own using the same Avatar
Creator API's we provide.

## Account Creation Element

This element is useful for creating an Account Creation UI. 

**Prefab Location:**
`Runtime/AvatarCreator/Prefabs/AccountCreationElement.prefab`.

**Key features include:**
- an input field for the user to enter their email address
- a button to send an account creation sign up email to the user 
- a button to continue without signup

If you view this AccountCreationElement in the unity inspector you will find a number of assignable fields and 2 events
that can be easily subscribed to. 
The first event OnSendEmail, is fired when the send button is clicked.
It also has a string parameter that can be used to retrieve the email address entered by the user if needed.

The second event OnContinue, is fired when the skip button is clicked.

Both of these are UnityEvents so they can be subscribed to in the inspector using the `+` symbol or in code.

## Asset Selection Element

This element, useful for creating asset selection UI panels or windows.

_NOTE: for creating color selection panels we suggest using the ColorSelectionElement instead_

**Prefab Location:**
`Runtime/AvatarCreator/Prefabs/AssetSelectionElement.prefab`.

This is best used for assets of the following asset types:
- BeardStyle
- Shapes (EyeShape, LipShape, etc)
- Facewear
- Headwear
- Clothing (Outfit, Shirt, Bottom, Top, etc)
- Facemask
- Glasses
- EyeColor **(this is a special case as eye color is actually a texture asset, not simply a color value)**

It is not recommended to use this for Color selection or Template avatar selection, for these we recommend the ColorSelectionElement & TemplateSelectionElement.

### Inspector Properties

If you find and open the AssetSelectionElement prefab we can go through the properties you see in the inspector.

**ButtonElementPrefab:** This is the prefab that will be used to display each asset.

**ButtonContainer:** This is the transform that the ButtonElementPrefabs will be instantiated under.

**SelectedIcon:** This is the icon that will be displayed on the selected asset to indicate to the user which one is currently selected. NOTE: This should exist already as a child of the AssetSelectionPrefab.

**OnAssetSelected:** This is a UnityEvent that will be fired when an asset is selected. It has a single parameter of type AvatarCreatorAsset. This can be used to retrieve the asset that was selected.

**BodyType:** This is the body type that the assets should be filtered by. This is used to ensure that only assets that are compatible with the selected body type are displayed.

**AssetType:** This is the asset type that the assets should be filtered by. This is used to determine which assets should be displayed and equipped.

**IconSize:** This is the square dimension size of the icon that will be requested. NOTE: bigger sizes will result in larger file sizes.

### Prefab Setup

You can add the AssetSelectionElement to your scene by following these steps.
1. Find the AssetSelectionElement prefab in the Unity Project window.
2. Drag the AssetSelectionElement prefab into your scene. NOTE: It should be placed as a child of a Canvas object as it is a UI element
3. Find the AssetSelectionElement in the hierarchy and select it to open the inspector.
4. From here you will need to set your Asset Type 
5. Set your body type (this can also be done at runtime using the SetBodyType function). 
6. You can also adjust any other settings as needed. 
7. Lastly you will see an OnAssetSelected event. You can subscribe to this event by clicking the `+`.

#### OnAssetSelected event

This event is called whenever an asset button is clicked (selected) and it has a single parameter of IAssetData.
The purpose of this event is to allow you to retrieve the asset ID and AssetType from other scripts. With this data you 
can make requests to our API's to update the avatar customizations. For example changing hair styles.

## Color Selection Element

This element is useful for creating a color selection UI panels.

_NOTE: This is not recommended for selecting eye color as it is actually a texture asset, not a color value,
for this you instead need to use the AssetSelectionElement._

**Prefab Location:**
`Runtime/AvatarCreator/Prefabs/ColorSelectionElement.prefab`.

This is best used for assets of the following asset types:
- SkinColor
- HairColor
- EyeColor
- LipColor
- EyebrowColor
- BeardColor

### Prefab Setup

You can add the ColorSelectionElement to your scene by following these steps.
1. Find the ColorSelectionElement prefab in the Unity Project window.
2. Drag the ColorSelectionElement prefab into your scene. NOTE: It should be placed as a child of a Canvas object as it is a UI element
3. Find the ColorSelectionElement in the hierarchy and select it to open the inspector.
4. From here you will need to set your Asset Type
5. By default all the properties on the ColorSelectionElement component should be set already however you can also adjust them as you like.
6. Lastly you will see an OnAssetSelected event. You can subscribe to this event by clicking the `+

## Template Selection Element

This element is useful for creating a template selection UI panels.

**Prefab Location:**
`Runtime/AvatarCreator/Prefabs/TemplateSelectionElement.prefab`.

### Prefab Setup

You can add the AssetSelectionElement to your scene by following these steps.
1. Find the TemplateSelectionElement prefab in the Unity Project window.
2. Drag the TemplateSelectionElement prefab into your scene. NOTE: It should be placed as a child of a Canvas object as it is a UI element
3. Find the TemplateSelectionElement in the hierarchy and select it to open the inspector.
4. By default all the properties on the TemplateSelectionElement component should be set already however you can also adjust them as you like.
5. Lastly you will see an OnAssetSelected event. You can subscribe to this event by clicking the `+`.

## Avatar Preview Element

The AvatarPreviewElement is prefab that can be used to load an avatar during the avatar creation process and it adds a number of useful features. 

**Prefab Location:**
`Runtime/AvatarCreator/Prefabs/AvatarPreviewElement.prefab`.

**Key features include:**
- AvatarPreviewElement component that stores a reference to the avatar and overrides when a new one is loaded
- CameraFocuser component that can be used to switch the camera focus between the avatar face and full body viewpoints
- AvatarRotator component for rotating the avatar
- MouseRotationHandler component that implements the IAvatarRotatorInput interface for handing mouseInput

## Gender Selection Element

This element is useful for creating a gender selection UI. 

**Prefab Location:**
`Runtime/AvatarCreator/Prefabs/GenderSelectionElement.prefab`.

**Key features include:**
- 2 buttons (male, female)
- A OnGenderSelected UnityEvent that can be subscribed to in the inspector or in code to retrieve the selected gender.

## Login Element

This element is useful for creating Ready Player Me login UI.

**Prefab Location:**
`Runtime/AvatarCreator/Prefabs/LoginElement.prefab`.

**Key features include:**
- an input field for the user to enter their email address
- an input field for the user to enter in their 1 time login code
- functionality to automatically send a login code to the user after they enter their email address
- functionality to merge user session to their Ready Player Me account
- OnLoginSuccess event that can be subscribed to in the inspector
- OnLoginFailed event that can be subscribed to in the inspector and passes a string containing the error message

## Logout Element

This element is useful for creating Ready Player Me user logging out UI.

**Prefab Location:**
`Runtime/AvatarCreator/Prefabs/LogoutElement.prefab`.

**Key features include:**
- a button, that user can click to log them out
- OnLogoutSuccess event that can be subscribed to in the inspector
- OnLogoutFailed event that can be subscribed to in the inspector and passes a string containing the error message

## Photo Capture Element

This element is useful for creating a photo capture UI and returning the image (selfie) of the user as a Texture2D so
that it can later be used to create a new avatar.

**Prefab Location:**
`Runtime/AvatarCreator/Prefabs/PhotoCaptureElement.prefab`.

## Avatar List Element

This element is useful for creating Ready Player Me UI for showing user avatars.

**Prefab Location:**
`Runtime/AvatarCreator/Prefabs/AvatarListElement.prefab`.

**Key features include:**
- A script, that will create prefabs with AvatarListItem script to the specific container (Example contains scrollview container)
- Filter, that allows you to select, if you want to see all of the avatars or only avatars created under the application
- OnAvatarSelect event that passes an avatar string, that was selected
- OnAvatarModify event that passes an avatar string, that was selected for modification
- OnAvatarDeletionStarted event that passes an avatar string, that was selected for deletion (On this element, we have also attached the DeleteAvatarElement, so when the user click on delete, then it shows avatarDeletionElement)
- onAvatarsLoaded event which passes an array of avatarIds, that were loaded
- public function RemoveItem, that deletes item with specific avatar id, that was initialized

## Avatar Deletion Element

This element is useful for creating Ready Player Me UI for popup to delete specific user avatars.

**Prefab Location:**
`Runtime/AvatarCreator/Prefabs/DeleteAvatarElement.prefab`.

**Key features include:**
- a Button to confirm the deletion
- a Button to cancel the deletion
- OnCancel event that passes the avatar string
- OnConfirm event that passes the avatar string after deleting the avatar
- OnError event that passes the error string, when something went wrong with deletion

## Selfie To Avatar Element

This a higher level element is useful for creating a UI that not only enables photo capture, it also uses the selected
photo to request an avatar before finally invoking the OnAvatarCreated event passing the GameObject and AvatarProperties.

**Prefab Location:**
`Runtime/AvatarCreator/Prefabs/SelfieToAvatarElement.prefab`.

## ButtonElement

This element is useful for creating a UI button that can be used to select an asset. 
The ButtonElement prefab has the image set to a loading icon by default so that it is seen to be loading, until the
asset has been loaded.
It uses the SelectionButton script that provides functionality for:
- Adding a listener to the button's OnClick event
- Setting the button's icon
- Setting the button color

## ColorButton

This element is useful for creating a UI button that can be used to select a color of an asset. 
The ColorButton prefab has the image set to white circle icon, it is white so that the tint or color value can be set
using the SetColor function on the SelectionButton script. 


## UserAvatarElement

This element is used in the avatar list to show avatar in the AvatarListElement. 
This element contains 2 fields:

- Button Actions - Array of actions for the buttons, that the avatarListElement is listening to. Currently, it has Delete, Select, and Customize actions.
- Avatar Image - RawImage

This prefab is initialized in the AvatarListElement and it listens to the onclick events for the buttons above.
This is fully customizable. For example, if you don't need the deletion functionality, then you don't add the delete button to the button actions list and the button click events are not registered for listening and AvatarListElement doesn't send out OnAvatarDeletionStared events.
