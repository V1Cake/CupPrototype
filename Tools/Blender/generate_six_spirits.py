import bpy
import math
import os
from mathutils import Vector


OUT_DIR = r"C:\workzone\unity_proj\BlenderModel\Bottles"
MASTER = os.path.join(OUT_DIR, "Bottle_SixSpirits_Master.blend")
SPIRITS = ("Vodka", "Gin", "Rum", "Tequila", "Whiskey", "Brandy")
X_POS = dict(zip(SPIRITS, (-0.375, -0.225, -0.075, 0.075, 0.225, 0.375)))


def clear_file():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for collection in list(bpy.data.collections):
        bpy.data.collections.remove(collection)
    for material in list(bpy.data.materials):
        bpy.data.materials.remove(material)


def material(name, color, roughness=0.25, metallic=0.0, transmission=0.0):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = color
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = color
    bsdf.inputs["Roughness"].default_value = roughness
    bsdf.inputs["Metallic"].default_value = metallic
    transmission_input = bsdf.inputs.get("Transmission Weight") or bsdf.inputs.get("Transmission")
    if transmission_input:
        transmission_input.default_value = transmission
    return mat


def make_glass_translucent(mat):
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Alpha"].default_value = 0.24
    if hasattr(mat, "surface_render_method"):
        mat.surface_render_method = "DITHERED"


def lathe_mesh(name, profile, sides=16):
    verts = []
    for radius, z in profile:
        for i in range(sides):
            angle = 2.0 * math.pi * i / sides
            verts.append((radius * math.cos(angle), radius * math.sin(angle), z))
    faces = []
    rings = len(profile)
    for ring in range(rings - 1):
        a = ring * sides
        b = (ring + 1) * sides
        for i in range(sides):
            j = (i + 1) % sides
            faces.append((a + i, a + j, b + j, b + i))
    faces.append(tuple(reversed(range(sides))))
    top = (rings - 1) * sides
    faces.append(tuple(top + i for i in range(sides)))
    mesh = bpy.data.meshes.new(name + "_Mesh")
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    return mesh


def rounded_rect_points(width, depth, corner):
    x, y, r = width / 2.0, depth / 2.0, corner
    return (
        (x - r, -y), (x, -y + r), (x, y - r), (x - r, y),
        (-x + r, y), (-x, y - r), (-x, -y + r), (-x + r, -y),
    )


def loft_mesh(name, profile):
    verts = []
    for width, depth, corner, z in profile:
        verts.extend((x, y, z) for x, y in rounded_rect_points(width, depth, corner))
    sides = 8
    faces = []
    for ring in range(len(profile) - 1):
        a, b = ring * sides, (ring + 1) * sides
        for i in range(sides):
            j = (i + 1) % sides
            faces.append((a + i, a + j, b + j, b + i))
    faces.append(tuple(reversed(range(sides))))
    top = (len(profile) - 1) * sides
    faces.append(tuple(top + i for i in range(sides)))
    mesh = bpy.data.meshes.new(name + "_Mesh")
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    return mesh


def finish_object(obj, collection, mat, bevel=0.0015):
    collection.objects.link(obj)
    obj.data.materials.append(mat)
    obj.rotation_euler = (0.0, 0.0, 0.0)
    obj.scale = (1.0, 1.0, 1.0)
    for polygon in obj.data.polygons:
        polygon.use_smooth = True
    if bevel:
        modifier = obj.modifiers.new("Bevel_2mm", "BEVEL")
        modifier.width = bevel
        modifier.segments = 2
        modifier.limit_method = "ANGLE"
        modifier.angle_limit = math.radians(30.0)
    return obj


def round_object(name, profile, sides, x, collection, mat, bevel=0.0015):
    obj = bpy.data.objects.new(name, lathe_mesh(name, profile, sides))
    obj.location = (x, 0.0, 0.0)
    return finish_object(obj, collection, mat, bevel)


def rect_object(name, profile, x, collection, mat, bevel=0.0015):
    obj = bpy.data.objects.new(name, loft_mesh(name, profile))
    obj.location = (x, 0.0, 0.0)
    return finish_object(obj, collection, mat, bevel)


def add_round_bottle(name, glass_profile, liquid_profile, cap_profile, sides, glass_mat, liquid_mat, cap_mat):
    collection = bpy.data.collections.new("Bottle_" + name)
    bpy.context.scene.collection.children.link(collection)
    x = X_POS[name]
    glass = round_object(name + "_Glass", glass_profile, sides, x, collection, glass_mat)
    liquid = round_object(name + "_Liquid", liquid_profile, sides, x, collection, liquid_mat, 0.0005)
    cap = round_object(name + "_Cap", cap_profile, sides, x, collection, cap_mat, 0.0012)
    for obj, role in ((glass, "Glass"), (liquid, "Liquid"), (cap, "Cap")):
        obj["spirit"] = name
        obj["role"] = role
        obj["pivot"] = "bottle_bottom_center"


def add_whiskey(glass_mat, liquid_mat, cap_mat):
    name = "Whiskey"
    collection = bpy.data.collections.new("Bottle_Whiskey")
    bpy.context.scene.collection.children.link(collection)
    x = X_POS[name]
    glass = rect_object("Whiskey_Glass", (
        (0.081, 0.061, 0.008, 0.000), (0.085, 0.065, 0.009, 0.007),
        (0.085, 0.065, 0.009, 0.168), (0.081, 0.061, 0.008, 0.183),
        (0.044, 0.044, 0.007, 0.205), (0.030, 0.030, 0.005, 0.220),
        (0.030, 0.030, 0.005, 0.232),
    ), x, collection, glass_mat)
    liquid = rect_object("Whiskey_Liquid", (
        (0.075, 0.055, 0.007, 0.005), (0.079, 0.059, 0.008, 0.011),
        (0.079, 0.059, 0.008, 0.164),
    ), x, collection, liquid_mat, 0.0005)
    cap = rect_object("Whiskey_Cap", (
        (0.034, 0.034, 0.005, 0.226), (0.036, 0.036, 0.006, 0.229),
        (0.036, 0.036, 0.006, 0.242), (0.034, 0.034, 0.005, 0.245),
    ), x, collection, cap_mat, 0.0012)
    for obj, role in ((glass, "Glass"), (liquid, "Liquid"), (cap, "Cap")):
        obj["spirit"] = name
        obj["role"] = role
        obj["pivot"] = "bottle_bottom_center"


def look_at(obj, point):
    obj.rotation_euler = (Vector(point) - obj.location).to_track_quat("-Z", "Y").to_euler()


def add_preview_rig(materials):
    rig = bpy.data.collections.new("Preview_Rig")
    bpy.context.scene.collection.children.link(rig)
    bpy.ops.mesh.primitive_plane_add(size=2.4, location=(0, 0, -0.003))
    floor = bpy.context.object
    floor.name = "Preview_Ground"
    for coll in list(floor.users_collection):
        coll.objects.unlink(floor)
    rig.objects.link(floor)
    floor.data.materials.append(materials["floor"])
    camera_data = bpy.data.cameras.new("Preview_Camera_Data")
    camera = bpy.data.objects.new("Preview_Camera", camera_data)
    rig.objects.link(camera)
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = 0.48
    bpy.context.scene.camera = camera
    for name, location, energy, size in (
        ("Key_Light", (-0.8, -1.1, 1.6), 120.0, 1.2),
        ("Fill_Light", (1.1, -0.3, 0.9), 80.0, 1.0),
        ("Rim_Light", (0.0, 0.8, 1.2), 100.0, 0.8),
    ):
        data = bpy.data.lights.new(name + "_Data", "AREA")
        data.energy, data.shape, data.size = energy, "DISK", size
        light = bpy.data.objects.new(name, data)
        light.location = location
        look_at(light, (0, 0, 0.13))
        rig.objects.link(light)
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 1200
    scene.render.resolution_y = 600
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.film_transparent = False
    scene.world.color = (0.035, 0.035, 0.045)
    return camera


def render_preview(camera, filename, location, target, ortho_scale):
    camera.location = location
    camera.data.ortho_scale = ortho_scale
    look_at(camera, target)
    bpy.context.scene.render.filepath = os.path.join(OUT_DIR, filename)
    bpy.ops.render.render(write_still=True)


def collection_bounds(collection):
    points = []
    for obj in collection.objects:
        if obj.type == "MESH":
            points.extend(obj.matrix_world @ Vector(corner) for corner in obj.bound_box)
    low = Vector((min(p.x for p in points), min(p.y for p in points), min(p.z for p in points)))
    high = Vector((max(p.x for p in points), max(p.y for p in points), max(p.z for p in points)))
    return low, high


def qa_and_stats():
    errors = []
    stats = {}
    depsgraph = bpy.context.evaluated_depsgraph_get()
    for spirit in SPIRITS:
        collection = bpy.data.collections.get("Bottle_" + spirit)
        if not collection:
            errors.append(spirit + ": missing collection")
            continue
        expected = {spirit + "_Glass", spirit + "_Liquid", spirit + "_Cap"}
        actual = {obj.name for obj in collection.objects}
        if expected != actual:
            errors.append(spirit + ": incorrect object set " + repr(actual))
        low, high = collection_bounds(collection)
        if abs(low.z) > 0.00001:
            errors.append(spirit + ": base is not Z=0")
        vertices = faces = 0
        for obj in collection.objects:
            if any(abs(v - 1.0) > 0.00001 for v in obj.scale):
                errors.append(obj.name + ": scale is not 1")
            if any(abs(v) > 0.00001 for v in obj.rotation_euler):
                errors.append(obj.name + ": rotation is not 0")
            if abs(obj.location.z) > 0.00001 or abs(obj.location.y) > 0.00001:
                errors.append(obj.name + ": pivot is not at bottle base center")
            evaluated = obj.evaluated_get(depsgraph)
            mesh = evaluated.to_mesh()
            vertices += len(mesh.vertices)
            faces += len(mesh.polygons)
            evaluated.to_mesh_clear()
        glass = bpy.data.objects[spirit + "_Glass"]
        liquid = bpy.data.objects[spirit + "_Liquid"]
        cap = bpy.data.objects[spirit + "_Cap"]
        if liquid.dimensions.x >= glass.dimensions.x or liquid.dimensions.y >= glass.dimensions.y:
            errors.append(spirit + ": liquid clearance failed")
        if cap.dimensions.x < glass_profile_neck_width(glass) - 0.0001:
            errors.append(spirit + ": cap does not cover neck")
        size = high - low
        stats[spirit] = {
            "dimensions_m": [round(size.x, 4), round(size.y, 4), round(size.z, 4)],
            "evaluated_vertices": vertices,
            "evaluated_faces": faces,
        }
    return errors, stats


def glass_profile_neck_width(glass):
    vertices = [v.co for v in glass.data.vertices]
    top = max(v.z for v in vertices)
    xs = [abs(v.x) for v in vertices if v.z > top - 0.001]
    return max(xs) * 2.0 if xs else 0.0


def save_individual_files():
    for spirit in SPIRITS:
        bpy.ops.wm.open_mainfile(filepath=MASTER)
        collection = bpy.data.collections["Bottle_" + spirit]
        for other in list(bpy.data.collections):
            if other != collection:
                bpy.data.collections.remove(other)
        for obj in collection.objects:
            obj.location.x = 0.0
        path = os.path.join(OUT_DIR, "Bottle_" + spirit + ".blend")
        bpy.ops.outliner.orphans_purge(do_recursive=True)
        bpy.ops.wm.save_as_mainfile(filepath=path, check_existing=False, compress=True)
    bpy.ops.wm.open_mainfile(filepath=MASTER)


def main():
    os.makedirs(OUT_DIR, exist_ok=True)
    clear_file()
    bpy.context.scene.unit_settings.system = "METRIC"
    bpy.context.scene.unit_settings.length_unit = "METERS"
    mats = {
        "glass": material("MAT_Glass_Preview", (0.08, 0.20, 0.26, 0.24), 0.24, transmission=0.05),
        "cap": material("MAT_Cap_Black", (0.018, 0.022, 0.028, 1.0), 0.2, metallic=0.18),
        "clear": material("MAT_Liquid_Clear", (0.78, 0.9, 0.96, 1.0), 0.18, transmission=0.08),
        "cool": material("MAT_Liquid_Clear_Cool", (0.55, 0.83, 0.78, 1.0), 0.2, transmission=0.06),
        "amber": material("MAT_Liquid_Amber", (0.72, 0.24, 0.045, 1.0), 0.2),
        "gold": material("MAT_Liquid_WarmGold", (0.88, 0.52, 0.10, 1.0), 0.22),
        "whiskey": material("MAT_Liquid_Whiskey", (0.58, 0.14, 0.025, 1.0), 0.2),
        "brandy": material("MAT_Liquid_Brandy", (0.34, 0.055, 0.018, 1.0), 0.2),
        "floor": material("MAT_Preview_Ground", (0.075, 0.085, 0.10, 1.0), 0.55),
    }
    make_glass_translucent(mats["glass"])
    add_round_bottle("Vodka", ((0.032, 0.0), (0.035, 0.006), (0.035, 0.190), (0.034, 0.205), (0.026, 0.224), (0.016, 0.239), (0.015, 0.263)), ((0.030, 0.005), (0.032, 0.010), (0.032, 0.185)), ((0.0185, 0.258), (0.019, 0.261), (0.019, 0.277), (0.0175, 0.280)), 16, mats["glass"], mats["clear"], mats["cap"])
    add_round_bottle("Gin", ((0.036, 0.0), (0.039, 0.006), (0.039, 0.172), (0.038, 0.190), (0.024, 0.207), (0.016, 0.216), (0.0155, 0.238)), ((0.033, 0.005), (0.036, 0.010), (0.036, 0.168)), ((0.019, 0.233), (0.020, 0.237), (0.020, 0.252), (0.018, 0.255)), 12, mats["glass"], mats["cool"], mats["cap"])
    add_round_bottle("Rum", ((0.038, 0.0), (0.041, 0.008), (0.041, 0.165), (0.039, 0.184), (0.032, 0.204), (0.019, 0.220), (0.016, 0.235)), ((0.035, 0.005), (0.038, 0.011), (0.038, 0.160)), ((0.019, 0.231), (0.020, 0.235), (0.020, 0.252), (0.018, 0.255)), 14, mats["glass"], mats["amber"], mats["cap"])
    add_round_bottle("Tequila", ((0.041, 0.0), (0.045, 0.010), (0.045, 0.145), (0.043, 0.165), (0.030, 0.181), (0.019, 0.190), (0.017, 0.207)), ((0.037, 0.007), (0.041, 0.014), (0.041, 0.141)), ((0.021, 0.201), (0.022, 0.205), (0.022, 0.222), (0.020, 0.225)), 12, mats["glass"], mats["gold"], mats["cap"])
    add_whiskey(mats["glass"], mats["whiskey"], mats["cap"])
    add_round_bottle("Brandy", ((0.036, 0.0), (0.044, 0.007), (0.0475, 0.035), (0.0475, 0.110), (0.044, 0.145), (0.035, 0.172), (0.023, 0.193), (0.016, 0.207), (0.015, 0.224)), ((0.032, 0.005), (0.040, 0.011), (0.044, 0.038), (0.044, 0.107), (0.040, 0.140), (0.032, 0.165)), ((0.019, 0.219), (0.020, 0.223), (0.020, 0.237), (0.018, 0.240)), 16, mats["glass"], mats["brandy"], mats["cap"])
    camera = add_preview_rig(mats)
    for obj in bpy.context.scene.objects:
        obj.select_set(False)
    errors, stats = qa_and_stats()
    if errors:
        raise RuntimeError("QA failed: " + " | ".join(errors))
    bpy.context.scene["QA_Status"] = "PASS"
    bpy.context.scene["QA_Stats"] = repr(stats)
    bpy.context.scene["Reference_File"] = r"C:\workzone\unity_proj\BlenderModel\Bottle_High_Base.blend (read-only)"
    render_preview(camera, "Bottle_SixSpirits_Front.png", (0.0, -1.6, 0.22), (0.0, 0.0, 0.125), 0.92)
    render_preview(camera, "Bottle_SixSpirits_ThreeQuarter.png", (0.9, -1.45, 0.52), (0.0, 0.0, 0.12), 1.05)
    bpy.ops.wm.save_as_mainfile(filepath=MASTER, check_existing=False, compress=True)
    save_individual_files()
    print({"master": MASTER, "qa": "PASS", "stats": stats})


main()
