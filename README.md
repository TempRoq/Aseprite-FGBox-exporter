# Aseprite-FGBox-exporter

Will look for the existence of hitbox, hurtbox, and pushbox layers, parse them for squares of different colors, and will export found square data into a txt file.

Example output
                                            TL Corner  BR Corner
Tag   Dur  type                   Color          VV      VV
Idle,{0.25{HITBOX,{}}{HURTBOX,{[[251,242,54,255]<7,0>,<13,5>],[[55,148,110,255]<7,6>,<13,15>],[[63,63,116,255]<7,16>,<13,19>]}}{PUSHBOX,{[[106,190,48,255]<7,4>,<13,19>]}}}

5L,{0.25{HITBOX,{}}{HURTBOX,{[[251,242,54,255]<7,0>,<13,5>],[[55,148,110,255]<7,6>,<13,15>],[[63,63,116,255]<7,16>,<13,19>]}}{PUSHBOX,{[[106,190,48,255]<7,4>,<13,19>]}}}{0.5{HITBOX,{[[172,50,50,255]<2,8>,<6,12>],[[217,87,99,255]<14,8>,<18,12>]}}{HURTBOX,{[[251,242,54,255]<8,4>,<15,9>],[[55,148,110,255]<5,6>,<15,15>],[[63,63,116,255]<5,16>,<15,19>]}}{PUSHBOX,{[[106,190,48,255]<7,8>,<13,19>]}}}{0.25{HITBOX,{[[172,50,50,255]<8,0>,<12,6>],[[217,87,99,255]<8,6>,<12,15>]}}{HURTBOX,{[[251,242,54,255]<7,0>,<13,5>],[[55,148,110,255]<7,6>,<13,15>],[[63,63,116,255]<7,16>,<13,19>]}}{PUSHBOX,{[[106,190,48,255]<7,4>,<13,19>]}}}

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
+ File will be outputted to the scripts folder. To find it, go to File > Scripts > Open Scripts Folder
