#pragma once
#include "WindowsCommon.h"
#include "VulkanGraphicsService.h"

VkFence VulkanCreateFence(VkDevice device)
{
	VkFenceCreateInfo createInfo = { VK_STRUCTURE_TYPE_FENCE_CREATE_INFO };

	VkFence fence = nullptr;
	AssertIfFailed(vkCreateFence(device, &createInfo, 0, &fence));

	return fence;
}

VkDeviceQueueCreateInfo CreateDeviceQueueCreateInfo(uint32_t queueFamilyIndex, uint32_t count)
{
    float* queuePriorities = new float[count];

    for (int i = 0; i < count; i++)
    {
        queuePriorities[i] = 1.0f;
    }

    VkDeviceQueueCreateInfo queueCreateInfo = { VK_STRUCTURE_TYPE_DEVICE_QUEUE_CREATE_INFO };
    queueCreateInfo.pQueuePriorities = queuePriorities;
    queueCreateInfo.queueCount = count;
    queueCreateInfo.queueFamilyIndex = queueFamilyIndex;

    return queueCreateInfo;
}

VkFormat VulkanConvertTextureFormat(GraphicsTextureFormat textureFormat, bool noSrgb = false) 
{
	switch (textureFormat)
	{
		case GraphicsTextureFormat::Bgra8UnormSrgb:
			return noSrgb ? VK_FORMAT_B8G8R8A8_UNORM : VK_FORMAT_B8G8R8A8_SRGB;
	
		case GraphicsTextureFormat::Depth32Float:
			return VK_FORMAT_D32_SFLOAT;
	
		case GraphicsTextureFormat::Rgba16Float:
			return VK_FORMAT_R16G16B16A16_SFLOAT;
	
		case GraphicsTextureFormat::R16Float:
			return VK_FORMAT_R16_SFLOAT;
	
		case GraphicsTextureFormat::BC1Srgb:
			return VK_FORMAT_BC1_RGBA_SRGB_BLOCK;

		case GraphicsTextureFormat::BC2Srgb:
			return VK_FORMAT_BC2_SRGB_BLOCK;

		case GraphicsTextureFormat::BC3Srgb:
			return noSrgb ? VK_FORMAT_BC3_UNORM_BLOCK : VK_FORMAT_BC3_SRGB_BLOCK;

		case GraphicsTextureFormat::BC4:
			return VK_FORMAT_BC4_UNORM_BLOCK;

		case GraphicsTextureFormat::BC5:
			return VK_FORMAT_BC5_UNORM_BLOCK;

		case GraphicsTextureFormat::BC6:
			return VK_FORMAT_BC6H_UFLOAT_BLOCK;

		case GraphicsTextureFormat::BC7Srgb:
			return VK_FORMAT_BC7_SRGB_BLOCK;

		case GraphicsTextureFormat::Rgba32Float:
			return VK_FORMAT_R32G32B32A32_SFLOAT;

		case GraphicsTextureFormat::Rgba16Unorm:
			return VK_FORMAT_R16G16B16A16_UNORM;
	}
        
	return noSrgb ? VK_FORMAT_R8G8B8A8_UNORM : VK_FORMAT_R8G8B8A8_SRGB;
}

VkImage CreateImage(VkDevice device, enum GraphicsTextureFormat textureFormat, enum GraphicsTextureUsage usage, int width, int height, int faceCount, int mipLevels, int multisampleCount)
{
	VkImageCreateInfo createInfo = { VK_STRUCTURE_TYPE_IMAGE_CREATE_INFO };
    createInfo.imageType = VK_IMAGE_TYPE_2D;
    createInfo.format = VulkanConvertTextureFormat(textureFormat);
    createInfo.extent.width = width;
    createInfo.extent.height = height;
    createInfo.extent.depth = 1;
    createInfo.mipLevels = mipLevels;
    createInfo.arrayLayers = faceCount;
    createInfo.samples = VK_SAMPLE_COUNT_1_BIT;
    createInfo.initialLayout = VK_IMAGE_LAYOUT_UNDEFINED;

    if (usage == GraphicsTextureUsage::ShaderRead)
    {
        createInfo.usage = VK_IMAGE_USAGE_SAMPLED_BIT | VK_IMAGE_USAGE_TRANSFER_DST_BIT;
    }

    else if (usage == GraphicsTextureUsage::RenderTarget && textureFormat == GraphicsTextureFormat::Depth32Float)
    {
        createInfo.usage = VK_IMAGE_USAGE_SAMPLED_BIT | VK_IMAGE_USAGE_DEPTH_STENCIL_ATTACHMENT_BIT;
    }

    else if (usage == GraphicsTextureUsage::RenderTarget)
    {
        createInfo.usage = VK_IMAGE_USAGE_SAMPLED_BIT | VK_IMAGE_USAGE_COLOR_ATTACHMENT_BIT;
    }

	VkImage image = nullptr;
    AssertIfFailed(vkCreateImage(device, &createInfo, nullptr, &image));

	return image;
}

VkImageView CreateImageView(VkDevice device, VkImage image, VkFormat textureFormat)
{
	VkImageViewCreateInfo createInfo = { VK_STRUCTURE_TYPE_IMAGE_VIEW_CREATE_INFO };
	createInfo.image = image;
	createInfo.viewType = VK_IMAGE_VIEW_TYPE_2D;
	createInfo.format = textureFormat;
	createInfo.subresourceRange.aspectMask = (textureFormat != VK_FORMAT_D32_SFLOAT) ? VK_IMAGE_ASPECT_COLOR_BIT : VK_IMAGE_ASPECT_DEPTH_BIT;
	createInfo.subresourceRange.levelCount = 1;
	createInfo.subresourceRange.layerCount = 1;

	VkImageView view = nullptr;
	AssertIfFailed(vkCreateImageView(device, &createInfo, 0, &view));

	return view;
}

VkFramebuffer CreateFramebuffer(VkDevice device, VkRenderPass renderPass, VkImageView imageView, uint32_t width, uint32_t height)
{
	VkFramebufferCreateInfo createInfo = { VK_STRUCTURE_TYPE_FRAMEBUFFER_CREATE_INFO };
	createInfo.renderPass = renderPass;
	createInfo.attachmentCount = 1;
	createInfo.pAttachments = &imageView;
	createInfo.width = width;
	createInfo.height = height;
	createInfo.layers = 1;

	VkFramebuffer framebuffer = nullptr;
	AssertIfFailed(vkCreateFramebuffer(device, &createInfo, 0, &framebuffer));

	return framebuffer;
}

// TODO: Pass the struct as a pointer
VkRenderPass CreateRenderPass(VkDevice device, struct GraphicsRenderPassDescriptor renderPassDescriptor)
{
	// TODO: Handle the proper amount of attachments
    VkAttachmentDescription attachments[1] = {};

	// TODO: Handle RT operations defined in the renderPassDescriptor
	attachments[0].format = VulkanConvertTextureFormat(renderPassDescriptor.RenderTarget1TextureFormat.Value);
	attachments[0].samples = VK_SAMPLE_COUNT_1_BIT;
	attachments[0].loadOp = VK_ATTACHMENT_LOAD_OP_CLEAR;
	attachments[0].storeOp = VK_ATTACHMENT_STORE_OP_STORE;
	attachments[0].stencilLoadOp = VK_ATTACHMENT_LOAD_OP_DONT_CARE;
	attachments[0].stencilStoreOp = VK_ATTACHMENT_STORE_OP_DONT_CARE;
	attachments[0].initialLayout = VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL;
	attachments[0].finalLayout = VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL;

	VkAttachmentReference colorAttachments = { 0, VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL };

	VkSubpassDescription subpass = {};
	subpass.pipelineBindPoint = VK_PIPELINE_BIND_POINT_GRAPHICS;
	subpass.colorAttachmentCount = 1;
	subpass.pColorAttachments = &colorAttachments;

	VkRenderPassCreateInfo createInfo = { VK_STRUCTURE_TYPE_RENDER_PASS_CREATE_INFO };
	createInfo.attachmentCount = 1;
	createInfo.pAttachments = attachments;
	createInfo.subpassCount = 1;
	createInfo.pSubpasses = &subpass;

	VkRenderPass renderPass = nullptr;
	AssertIfFailed(vkCreateRenderPass(device, &createInfo, 0, &renderPass));
	return renderPass;
}

VkDescriptorSetLayout CreateDescriptorSetLayout(VkDevice device, VkDescriptorType descriptorType, uint32_t descriptorCount, bool isPushDescriptor = false)
{
	VkDescriptorBindingFlags flags = {};
	flags = VK_DESCRIPTOR_BINDING_VARIABLE_DESCRIPTOR_COUNT_BIT | VK_DESCRIPTOR_BINDING_PARTIALLY_BOUND_BIT;

	VkDescriptorSetLayoutBindingFlagsCreateInfo binding_flags{};
	binding_flags.sType = VK_STRUCTURE_TYPE_DESCRIPTOR_SET_LAYOUT_BINDING_FLAGS_CREATE_INFO;
	binding_flags.bindingCount = 1;
	binding_flags.pBindingFlags = &flags;

	// TODO: To replace with dynamic shader discovery
	VkDescriptorSetLayoutBinding descriptorBinding = {};
	descriptorBinding.binding = 0;
	descriptorBinding.descriptorType = descriptorType;
	descriptorBinding.descriptorCount = descriptorCount;
	descriptorBinding.stageFlags = VK_SHADER_STAGE_ALL_GRAPHICS;

	VkDescriptorSetLayoutCreateInfo descriptorSetCreateInfo = { VK_STRUCTURE_TYPE_DESCRIPTOR_SET_LAYOUT_CREATE_INFO };
	// descriptorSetCreateInfo.flags = VK_DESCRIPTOR_SET_LAYOUT_CREATE_PUSH_DESCRIPTOR_BIT_KHR;
	descriptorSetCreateInfo.flags = VK_DESCRIPTOR_SET_LAYOUT_CREATE_UPDATE_AFTER_BIND_POOL_BIT;
	descriptorSetCreateInfo.bindingCount = 1;
	descriptorSetCreateInfo.pBindings = &descriptorBinding;
	descriptorSetCreateInfo.pNext = &binding_flags;

	VkDescriptorSetLayout setLayout = nullptr;
	AssertIfFailed(vkCreateDescriptorSetLayout(device, &descriptorSetCreateInfo, 0, &setLayout));

	return setLayout;
}

VkDescriptorSetLayout globalBufferLayout = nullptr;

VkDescriptorSetLayout GetGlobalBufferLayout(VkDevice device)
{
	if (globalBufferLayout == nullptr)
	{
		globalBufferLayout = CreateDescriptorSetLayout(device, VK_DESCRIPTOR_TYPE_STORAGE_BUFFER, 1000);
	}

	return globalBufferLayout;
}

VkDescriptorSetLayout globalTextureLayout = nullptr;

VkDescriptorSetLayout GetGlobalTextureLayout(VkDevice device)
{
	if (globalTextureLayout == nullptr)
	{
		globalTextureLayout = CreateDescriptorSetLayout(device, VK_DESCRIPTOR_TYPE_SAMPLED_IMAGE, 1000);
	}

	return globalTextureLayout;
}

VkPipelineLayout CreateGraphicsPipelineLayout(VkDevice device, uint32_t* layoutCount, VkDescriptorSetLayout** outputSetLayouts)
{
	// TODO: To replace with dynamic shader discovery
	VkDescriptorSetLayout setLayouts[] =
	{
		GetGlobalBufferLayout(device),
		GetGlobalTextureLayout(device),
		CreateDescriptorSetLayout(device, VK_DESCRIPTOR_TYPE_SAMPLER, 1)
	};

	VkPipelineLayoutCreateInfo layoutCreateInfo = { VK_STRUCTURE_TYPE_PIPELINE_LAYOUT_CREATE_INFO };
	layoutCreateInfo.pSetLayouts = setLayouts;
	layoutCreateInfo.setLayoutCount = ARRAYSIZE(setLayouts);

	// TODO: 
	VkPushConstantRange push_constant;
	push_constant.offset = 0;
	push_constant.size = 4;
	push_constant.stageFlags = VK_SHADER_STAGE_ALL_GRAPHICS;

	layoutCreateInfo.pPushConstantRanges = &push_constant;
	layoutCreateInfo.pushConstantRangeCount = 1;

	VkPipelineLayout layout = 0;
	AssertIfFailed(vkCreatePipelineLayout(device, &layoutCreateInfo, 0, &layout));

	*outputSetLayouts = setLayouts;
	*layoutCount = 4;

	return layout;
}

VkPipeline CreateGraphicsPipeline(VkDevice device, VkRenderPass renderPass, VkPipelineLayout layout, VulkanShader* shader)
{
	VkGraphicsPipelineCreateInfo createInfo = { VK_STRUCTURE_TYPE_GRAPHICS_PIPELINE_CREATE_INFO };

	VkPipelineShaderStageCreateInfo stages[2] = {};
	stages[0].sType = VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO;
	stages[0].stage = VK_SHADER_STAGE_MESH_BIT_NV;
	stages[0].module = shader->MeshShaderMethod;
	stages[0].pName = "MeshMain";
	stages[1].sType = VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO;
	stages[1].stage = VK_SHADER_STAGE_FRAGMENT_BIT;
	stages[1].module = shader->PixelShaderMethod;
	stages[1].pName = "PixelMain";

	createInfo.stageCount = sizeof(stages) / sizeof(stages[0]);
	createInfo.pStages = stages;

	VkPipelineViewportStateCreateInfo viewportState = { VK_STRUCTURE_TYPE_PIPELINE_VIEWPORT_STATE_CREATE_INFO };
	viewportState.viewportCount = 1;
	viewportState.scissorCount = 1;
	createInfo.pViewportState = &viewportState;

	VkPipelineRasterizationStateCreateInfo rasterizationState = { VK_STRUCTURE_TYPE_PIPELINE_RASTERIZATION_STATE_CREATE_INFO };
	rasterizationState.lineWidth = 1.0f;
	createInfo.pRasterizationState = &rasterizationState;

	VkPipelineMultisampleStateCreateInfo multisampleState = { VK_STRUCTURE_TYPE_PIPELINE_MULTISAMPLE_STATE_CREATE_INFO };
	multisampleState.rasterizationSamples = VK_SAMPLE_COUNT_1_BIT;
	createInfo.pMultisampleState = &multisampleState;

	VkPipelineDepthStencilStateCreateInfo depthStencilState = { VK_STRUCTURE_TYPE_PIPELINE_DEPTH_STENCIL_STATE_CREATE_INFO };
	createInfo.pDepthStencilState = &depthStencilState;

	VkPipelineColorBlendAttachmentState colorAttachmentState = {};
	colorAttachmentState.colorWriteMask = VK_COLOR_COMPONENT_R_BIT | VK_COLOR_COMPONENT_G_BIT | VK_COLOR_COMPONENT_B_BIT | VK_COLOR_COMPONENT_A_BIT;

	VkPipelineColorBlendStateCreateInfo colorBlendState = { VK_STRUCTURE_TYPE_PIPELINE_COLOR_BLEND_STATE_CREATE_INFO };
	colorBlendState.attachmentCount = 1;
	colorBlendState.pAttachments = &colorAttachmentState;
	createInfo.pColorBlendState = &colorBlendState;

	VkDynamicState dynamicStates[] = { VK_DYNAMIC_STATE_VIEWPORT, VK_DYNAMIC_STATE_SCISSOR };

	VkPipelineDynamicStateCreateInfo dynamicState = { VK_STRUCTURE_TYPE_PIPELINE_DYNAMIC_STATE_CREATE_INFO };
	dynamicState.dynamicStateCount = sizeof(dynamicStates) / sizeof(dynamicStates[0]);
	dynamicState.pDynamicStates = dynamicStates;
	createInfo.pDynamicState = &dynamicState;

	createInfo.layout = layout;
	createInfo.renderPass = renderPass;

	//TODO: Use the pipelinecache !
	VkPipeline pipeline = 0;
	AssertIfFailed(vkCreateGraphicsPipelines(device, nullptr, 1, &createInfo, 0, &pipeline));

	return pipeline;
}

VkShaderModule CreateShaderModule(VkDevice device, void* data, int dataLength)
{
	VkShaderModuleCreateInfo createInfo = { VK_STRUCTURE_TYPE_SHADER_MODULE_CREATE_INFO };
	createInfo.codeSize = dataLength;
	createInfo.pCode = reinterpret_cast<const uint32_t*>(data);

	VkShaderModule shaderModule = nullptr;
	AssertIfFailed(vkCreateShaderModule(device, &createInfo, 0, &shaderModule));

	return shaderModule;
}

VkImageMemoryBarrier CreateImageTransitionBarrier(VkImage image, VkImageLayout oldLayout, VkImageLayout newLayout, bool isDepthBuffer)
{
	// TODO: Handle other parameters

	VkImageMemoryBarrier result = { VK_STRUCTURE_TYPE_IMAGE_MEMORY_BARRIER };

	result.srcAccessMask = VK_ACCESS_NONE_KHR;
	result.dstAccessMask = VK_ACCESS_NONE_KHR;
	result.oldLayout = oldLayout;
	result.newLayout = newLayout;
	result.srcQueueFamilyIndex = VK_QUEUE_FAMILY_IGNORED;
	result.dstQueueFamilyIndex = VK_QUEUE_FAMILY_IGNORED;
	result.image = image;
	result.subresourceRange.aspectMask = isDepthBuffer ? VK_IMAGE_ASPECT_DEPTH_BIT : VK_IMAGE_ASPECT_COLOR_BIT;
	result.subresourceRange.levelCount = VK_REMAINING_MIP_LEVELS;
	result.subresourceRange.layerCount = VK_REMAINING_ARRAY_LAYERS;

	return result;
}

void TransitionTextureToState(VulkanCommandList* commandList, VulkanTexture* texture, VkImageLayout destinationState, bool isTransfer = false)
{
	if (texture->ResourceState != destinationState)
	{
		auto barrier = CreateImageTransitionBarrier(texture->TextureObject, texture->ResourceState, destinationState, texture->Format == VK_FORMAT_D32_SFLOAT);

		if (!isTransfer)
		{
			vkCmdPipelineBarrier(commandList->CommandBufferObject, VK_PIPELINE_STAGE_COLOR_ATTACHMENT_OUTPUT_BIT, VK_PIPELINE_STAGE_COLOR_ATTACHMENT_OUTPUT_BIT, VK_DEPENDENCY_BY_REGION_BIT, 0, 0, 0, 0, 1, &barrier);
		}

		else
		{
			vkCmdPipelineBarrier(commandList->CommandBufferObject, VK_PIPELINE_STAGE_TRANSFER_BIT, VK_PIPELINE_STAGE_TRANSFER_BIT, VK_DEPENDENCY_BY_REGION_BIT, 0, 0, 0, 0, 1, &barrier);
		}

		texture->ResourceState = destinationState;
	}
}

PFN_vkVoidFunction GetVulkanFeatureFunction(VkInstance instance, VkDevice device, const char* name) 
{
	if (device != nullptr)
	{
		auto result = vkGetDeviceProcAddr(device, name);
		assert(result != nullptr);
		return result;
	}

	else
	{
		auto result = vkGetInstanceProcAddr(instance, name);
		assert(result != nullptr);

		return result;
	}
}

PFN_vkCmdDrawMeshTasksNV vkCmdDrawMeshTasks;
PFN_vkCreateDebugReportCallbackEXT vkCreateDebugReportCallback;
PFN_vkSetDebugUtilsObjectNameEXT vkSetDebugUtilsObjectName;
PFN_vkDestroyDebugReportCallbackEXT vkDestroyDebugReportCallback;

void InitVulkanFeatureFunctions(VkInstance instance, VkDevice device)
{
	vkCmdDrawMeshTasks = (PFN_vkCmdDrawMeshTasksNV)GetVulkanFeatureFunction(nullptr, device, "vkCmdDrawMeshTasksNV");

#ifdef DEBUG
	vkSetDebugUtilsObjectName = (PFN_vkSetDebugUtilsObjectNameEXT)GetVulkanFeatureFunction(nullptr, device, "vkSetDebugUtilsObjectNameEXT");
	vkCreateDebugReportCallback = (PFN_vkCreateDebugReportCallbackEXT)GetVulkanFeatureFunction(instance, nullptr, "vkCreateDebugReportCallbackEXT");
	vkDestroyDebugReportCallback = (PFN_vkDestroyDebugReportCallbackEXT)GetVulkanFeatureFunction(instance, nullptr, "vkDestroyDebugReportCallbackEXT");
#endif
}