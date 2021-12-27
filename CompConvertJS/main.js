const dpath = "C:\\Users\\kokoa\\Documents\\NeosDecomp\\FrooxEngine"
const components = require("./Component.json")
const fs = require("fs").promises
const path = require('path')

components.forEach(async c => {
    // console.log(c.fullName)
    const fullpath = path.join(dpath,...c.fullName.split("."))
    if(fullpath.includes("Business")) return;
    const data = await fs.readFile(fullpath+".cs", 'utf-8')

    const awakeindex = data.indexOf("OnAwake")
    if(awakeindex > 0 && !data.includes("OnAwake() =>")) {
        const endindex = data.indexOf("}",awakeindex)
        const awake = data.substring(awakeindex+16,endindex)
        
        if(awake.includes("{")) return

        console.log(awake)
    }
});
