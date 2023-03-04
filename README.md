# Aseprite-FGBox-exporter

Will look for the existence of hitbox, hurtbox, and pushbox layers, parse them for squares of different colors, and will export found square data into a txt file.

Example output
```
                                            TL Corner  BR Corner
Tag   Dur  type                   Color          VV      VV

Idle,{0.25{HITBOX,{}}{HURTBOX,{[[251,242,54,255]<7,0>,<13,5>],[[55,148,110,255]<7,6>,<13,15>],[[63,63,116,255]<7,16>,<13,19>]}}{PUSHBOX,{[[106,190,48,255]<7,4>,<13,19>]}}}
```



Constraints:
+ Must have layers with names:
  + "hitboxes"
  + "hurtboxes"
  + "pushboxes"
+ Each unique box within a layer must be a different color
+ In order to be parsed, a sprite must be within a tag.

Notes:
+ If a box's entire side is cut off by another box's side, the algorithm will assume that the missing side is shared with the overlapping side.
+ This does not append to the end of a file, it overwrites the file
+ File will be outputted wherever the sprite is currently saved to
