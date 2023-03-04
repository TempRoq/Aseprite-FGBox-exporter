local dlg = Dialog { title = "Hello World!"}

dlg:button{
    id = "export",
    label = "Export",
    text = "GO",
    selected = true,
    focus = true,
    onclick = function()
        local boxDataString = ExportBoxData()
        local path2 = app.activeSprite.filename:gsub(".aseprite", "_hitbox_data_output.txt")
        local file = io.open(path2, "w")
        if (file ~= nil) then
            file:write(boxDataString)
            file:close()
            print(boxDataString)
        end
        dlg:close()
    end
}

dlg:show { wait = false }



function FindCels(frame)
    local ret = {nil, nil, nil}
    for i = 1, #app.activeSprite.layers do
        local name = string.lower(app.activeSprite.layers[i].name)
        if name == "hitboxes" then
            ret[1] = app.activeSprite.layers[i]:cel(frame)
        elseif name == "hurtboxes" then
            ret[2] = app.activeSprite.layers[i]:cel(frame)
        elseif name == "pushboxes" then
            ret[3] = app.activeSprite.layers[i]:cel(frame)
        end
    end
    return ret
end

-- technique borrowed from https://behreajj.medium.com/how-to-script-aseprite-tools-in-lua-8f849b08733
BoxTypes = {HURT= -2, PUSH = -1, NRMLHIT = 0, GRAB = 1, FLIP = 2, STUN = 3, WIND = 4}

--Classes
Color = {}
Color.__index = Color

setmetatable(Color, {
    __call = function(cls, ...)
        return cls.new(...)
    end
})

function Color.new(red, green, blue, alpha)
    local inst = {}
    setmetatable(inst, Color)
    inst.r = red
    inst.g = green
    inst.b = blue
    inst.a = alpha
    return inst
end

function Color.toString(clr)
    return "["..clr.r..","..clr.g..","..clr.b..","..clr.a.."]"

end


Vec2 = {}
Vec2.__index = Vec2

setmetatable(Vec2, {
    __call = function (cls, ...)
        return cls.new(...)
    end
})


function Vec2.new(_x, _y)
    local inst = {}
    setmetatable(inst, Vec2)
    inst.x = _x or 0.0
    inst.y = _y or inst.x
    return inst
end


function Vec2.toString(val)
    return "<"..val.x..","..val.y..">"
end


Box = {}
Box.__index = Box

setmetatable(Box, {
    __call = function(cls, ...)
        return cls.new(...)
    end
})

function Box.new(_TL, _BR, _type) --Types = 
    local inst = {}
    setmetatable(inst, Box)
    inst.TL = _TL
    inst.BR = _BR
    inst.type = _type
    return inst
end

function Box.toString(box)
    return "["..Color.toString(box.type)..Vec2.toString(box.TL)..","..Vec2.toString(box.BR).."]"
end

function BoxArrayToString(frame)
    local ret = "{"
    if frame ~= nil then
        for i = 1, #frame do
            ret = ret..Box.toString(frame[i])
            if i ~= #frame then
                ret = ret..","
            end
        end
    end
    ret = ret.."}"
    return ret
end

--Send in the cel 


function FindBoxes(cel) --Get the Frame --> Layer --> 
    if (cel == nil) then
        return nil
    end
    local image = cel.image
    BoxParseData = {}
    BoxParseData.__index = BoxParseData
    setmetatable(BoxParseData, {
        __call = function(cls, ...)
            return cls.new
        end
    })

    function NewBoxParseData(x, y, t)
        local inst = {}
        setmetatable(inst, BoxParseData)
        inst.lowestX = x
        inst.highestX = x
        inst.lowestY = y
        inst.highestY = y
        inst.color = t
        return inst
    end

    local ret = {} --all BoxParseData
    local ind = {}
local var = 0
    for it in image:pixels() do
        local pixelVals = Color.new(app.pixelColor.rgbaR(it()), app.pixelColor.rgbaG(it()), app.pixelColor.rgbaB(it()), app.pixelColor.rgbaA(it()))
        local index = Color.toString(pixelVals)
        if pixelVals.a == 255 then
            if (ind[index] == nil) then
                var = var + 1
                ind[index] = var
                local bpd = NewBoxParseData(it.x, it.y, pixelVals)
                table.insert(ret, bpd)
            else
                if ret[ind[index]].lowestX > it.x then
                    ret[ind[index]].lowestX = it.x
                end
                if ret[ind[index]].highestX < it.x then
                    ret[ind[index]].highestX = it.x
                end

                if ret[ind[index]].lowestY > it.y then
                    ret[ind[index]].lowestY = it.y
                end

                if ret[ind[index]].highestY < it.y then
                    ret[ind[index]].highestY = it.y
                end
            end
        end
    end

    local ret2 = {} --gonna be the actual boxes that are returned
    for i, corners in ipairs(ret) do
        local newBox = Box.new(Vec2.new(corners.lowestX, corners.lowestY), Vec2.new(corners.highestX, corners.highestY), corners.color)
        local status = XLine(image, newBox.type, newBox.TL.x, newBox.TL.y, newBox.BR.x - newBox.TL.x) --Check to see if Top Left y bound should go up
        if status == 0 and newBox.TL.y ~= 0 then
            newBox.TL.y = newBox.TL.y - 1
        elseif status == 2 and newBox.TL.y ~= 0 then
            if app.pixelColor.rgbaA(image:getPixel(newBox.TL.x, newBox.TL.y-1)) == 255 or app.pixelColor.rgbaA(image:getPixel(newBox.BR.x, newBox.TL.y-1)) == 255 then
                newBox.TL.y = newBox.TL.y - 1
            end
        end
        status = XLine(image, newBox.type, newBox.TL.x, newBox.BR.y, newBox.BR.x - newBox.TL.x) --check to see if Bottom Right y bound should go down
        if status == 0 and newBox.BR.y ~= cel.bounds.height-1 then --make  horizontal line at the top left. If you hit an empty spot
            newBox.BR.y = newBox.BR.y + 1
            --print("Sugar we're goin down on status 1")
        elseif status == 2 and newBox.BR.y ~= cel.bounds.height-1 then
            if app.pixelColor.rgbaA(image:getPixel(newBox.TL.x, newBox.BR.y + 1)) == 255 or app.pixelColor.rgbaA(image:getPixel(newBox.BR.x, newBox.BR.y + 1)) == 255 then
                --print("Sugar we're going down on status 2 " .. cel.frameNumber.. " " .. cel.layer.name)
                newBox.BR.y = newBox.BR.y + 1
            end
        end
        status = YLine(image, newBox.type, newBox.TL.x, newBox.TL.y, newBox.BR.y - newBox.TL.y) --check to see if Leftmost x should go left
        if status == 0 and newBox.TL.x ~= 0 then
            newBox.TL.x = newBox.TL.x - 1
        elseif status == 2 and newBox.TL.x ~= 0 then
            if app.pixelColor.rgbaA(image:getPixel(newBox.TL.x - 1,newBox.TL.y)) == 255 or app.pixelColor.rgbaA(image:getPixel(newBox.TL.x-1, newBox.BR.y)) == 255 then
                newBox.TL.x = newBox.TL.x - 1
            end
        end
        status = YLine(image, newBox.type, newBox.BR.x, newBox.TL.y, newBox.BR.y - newBox.TL.y)
        if status == 0 and newBox.BR.x ~= cel.bounds.width-1 then
            newBox.BR.x = newBox.BR.x + 1
        elseif status == 2 and newBox.BR.x ~= cel.bounds.width-1 then
            if app.pixelColor.rgbaA(image:getPixel(newBox.BR.x + 1, newBox.TL.y)) == 255 or app.pixelColor.rgbaA(image:getPixel(newBox.BR.x + 1, newBox.BR.y)) == 255 then
                newBox.BR.x = newBox.BR.x + 1
            end
        end
        --Apply the cel bounds offset
        newBox.TL.x = newBox.TL.x + cel.bounds.x
        newBox.BR.x = newBox.BR.x + cel.bounds.x
        newBox.TL.y = newBox.TL.y + cel.bounds.y
        newBox.BR.y = newBox.BR.y + cel.bounds.y
        table.insert(ret2, newBox)
    end
    return ret2
end

--if it hits an empty space, return 0 (this is not valid, move up/down)
--if it hits a space of the same color, return 1 (this is valid)
--if it makes it to the end, return 2 (more tests needed)
function YLine(image, color, x, y, dist)
    for i=y+1, dist-2, 1 do
        if app.pixelColor.rgbaA(image:getPixel(x, i)) ~= 255 then
            return 0
        end

        if image:getPixel(x, i) == color then
            return 1
        end
    end
    return 2
end

function XLine(image, color, x, y, dist)
    for i = x+1, dist-2, 1 do

        if app.pixelColor.rgbaA(image:getPixel(i, y)) ~= 255 then
            return 0
        end

        if image:getPixel(i, y) == app.pixelColor.rgba(color.r, color.g, color.b, color.a) then
            return 1
        end
    end
    return 2
end


--Print Function will end up as "{"..
function ExportBoxData()
    local sprite = app.activeSprite
    local hitboxLayer
    local hurtboxLayer
    local pushboxLayer


    if #sprite.frames == 0 then --If no sprites, don't export anything
        return "NO FRAMES"
    end

    local retString = ""

    for a, tag in ipairs(sprite.tags) do
        --new animation, which is frame data + tag
        local currentFrame = tag.fromFrame
        local str = tag.name..","
        while currentFrame ~= tag.toFrame.next do
            local cels = FindCels(currentFrame)
            local hitboxes = FindBoxes(cels[1])
            local hurtboxes =  FindBoxes(cels[2])
            local pushboxes = FindBoxes(cels[3])
            str = str.."{"..currentFrame.duration.."{HITBOX,".. BoxArrayToString(hitboxes).."}{HURTBOX,"..BoxArrayToString(hurtboxes).."}{PUSHBOX,"..BoxArrayToString(pushboxes).."}}"
            currentFrame = currentFrame.next
        end
        retString = str.."\n"
    end

    return retString

end