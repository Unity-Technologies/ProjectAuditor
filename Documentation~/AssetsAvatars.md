<a name="AssetsAvatars"></a>
# Avatars View
The Avatars View shows all Avatar assets in the project's Assets folder, along with their properties and
asset import settings.

Note: The Packages folder is excluded from this scan; only the Assets folder is reviewed.

The table columns are as follows:

| Column Name      | Column Description                                                                                                                                                    | 
|------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **Name**             | The Avatar asset file name.                                                                                                                                           |
| **Valid?**           | Shows a tick if the Avatar is a valid mecanim avatar. It can be a generic avatar or a human avatar.                                                                   |
| **Human?**           | Shows a tick if the Avatar is a valid human avatar.                                                                                                                   |
| **Human Bones**      | If the Avatar is human, shows the number of mappings between Mecanim bones names and bone names in the rig.                                                           |
| **Skeleton Bones**   | If the Avatar is human, shows the number of bone Transforms in the model.                                                                                             |
| **Upper Arm Twist**  | If the Avatar is human, shows how much rotation is applied to the shoulder and elbow. A twist of 0 applies entirely to the shoulder, 1 applies entirely to the elbow. |
| **Lower Arm Twist**  | If the Avatar is human, shows how much rotation is applied to the wrist and elbow. A twist of 0 applies entirely to the elbow, 1 applies entirely to the wrist.       |                                                                                                                                        |
| **Upper Leg Twist**  | If the Avatar is human, shows how much rotation is applied to the thigh and knee. A twist of 0 applies entirely to the this, 1 applies entirely to the knee.          |                                                                                                                                        |
| **Lower Leg Twist**  | If the Avatar is human, shows how much rotation is applied to the knee and ankle. A twist of 0 applies entirely to the knee, 1 applies entirely to the ankle.         |                                                                                                                                        |
| **Arm Stretch**      | If the Avatar is human, shows the amount by which arms are allowed to stretch to reach targets when using Inverse Kinematics (IK).                                    |
| **Leg Stretch**      | If the Avatar is human, shows the amount by which legs are allowed to stretch to reach targets when using Inverse Kinematics (IK).                                    |                                                                                                                                        |
| **Feet Spacing**     | If the Avatar is human, shows the minimum distance between the avatar's feet during IK movement.                                                                      |
| **Translation DoF?** | Shows a tick if the Avatar is human and has a translation Degree of Freedom (DoF) on its Spine, Chest, Neck, Should or Upper Leg bones.                               |
| **Size**             | File size of the imported Avatar asset.                                                                                                                               |
| **Path**             | The full path to the source asset within the Assets folder.                                                                                                           |


