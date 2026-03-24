extends GutTest
## Sanity test to verify GUT framework and project setup.


func test_gut_works() -> void:
	assert_true(true, "GUT framework is operational")


func test_project_name() -> void:
	var name: String = ProjectSettings.get_setting("application/config/name")
	assert_eq(name, "A Dungeon in the Middle of Nowhere", "Project name matches")


func test_viewport_dimensions() -> void:
	var width: int = ProjectSettings.get_setting("display/window/size/viewport_width")
	var height: int = ProjectSettings.get_setting("display/window/size/viewport_height")
	assert_eq(width, 1920, "Viewport width is 1920")
	assert_eq(height, 1080, "Viewport height is 1080")


func test_renderer() -> void:
	var renderer: String = ProjectSettings.get_setting("rendering/renderer/rendering_method")
	assert_eq(renderer, "gl_compatibility", "Using GL Compatibility renderer")
