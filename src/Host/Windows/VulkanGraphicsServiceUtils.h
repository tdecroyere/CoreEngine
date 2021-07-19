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
    vector<float> queuePriorities = vector<float>(count);

    for (int i = 0; i < count; i++)
    {
        queuePriorities[i] = 1.0f;
    }

    VkDeviceQueueCreateInfo queueCreateInfo = { VK_STRUCTURE_TYPE_DEVICE_QUEUE_CREATE_INFO };
    queueCreateInfo.pQueuePriorities = queuePriorities.data();
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
		
		case GraphicsTextureFormat::R32Float:
			return VK_FORMAT_R32_SFLOAT;
	
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

VkBuffer VulkanCreateIndirectCommandWorkingBuffer(VkDevice device, VulkanShader* shader, VulkanPipelineState* pipelineState, uint32_t maxCommandCount, uint32_t gpuMemoryIndex, VkDeviceMemory* deviceMemory, uint32_t* workingBufferSize)
{
	VkGeneratedCommandsMemoryRequirementsInfoNV info { VK_STRUCTURE_TYPE_GENERATED_COMMANDS_MEMORY_REQUIREMENTS_INFO_NV };
	info.pipeline = pipelineState->PipelineStateObject;
	info.pipelineBindPoint = VK_PIPELINE_BIND_POINT_GRAPHICS;
	info.maxSequencesCount = maxCommandCount;
	info.indirectCommandsLayout = shader->CommandSignature;
	
	VkMemoryRequirements2 memoryRequirements = { VK_STRUCTURE_TYPE_MEMORY_REQUIREMENTS_2 };
	vkGetGeneratedCommandsMemoryRequirementsNV(device, &info, &memoryRequirements);

	VkBufferCreateInfo createInfo = { VK_STRUCTURE_TYPE_BUFFER_CREATE_INFO };
	createInfo.size = memoryRequirements.memoryRequirements.size;
	createInfo.usage = VK_BUFFER_USAGE_STORAGE_BUFFER_BIT;

	VkBuffer buffer = nullptr;
	AssertIfFailed(vkCreateBuffer(device, &createInfo, nullptr, &buffer));

	VkMemoryRequirements bufferMemoryRequirements;
	vkGetBufferMemoryRequirements(device, buffer, &bufferMemoryRequirements);

	VkMemoryAllocateInfo allocateInfo = { VK_STRUCTURE_TYPE_MEMORY_ALLOCATE_INFO };
	allocateInfo.memoryTypeIndex = gpuMemoryIndex;
	allocateInfo.allocationSize = bufferMemoryRequirements.size;

	vkAllocateMemory(device, &allocateInfo, nullptr, deviceMemory);
	AssertIfFailed(vkBindBufferMemory(device, buffer, *deviceMemory, 0));

	*workingBufferSize = memoryRequirements.memoryRequirements.size;

	return buffer;
}

VkImage CreateImage(VkDevice device, enum GraphicsTextureFormat textureFormat, enum GraphicsTextureUsage usage, int width, int height, int faceCount, int mipLevels, int multisampleCount)
{
	VkImageCreateInfo createInfo = { VK_STRUCTURE_TYPE_IMAGE_CREATE_INFO };
    createInfo.imageType = VK_IMAGE_TYPE_2D;
    createInfo.format = VulkanConvertTextureFormat(textureFormat, false);
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
	
	else if (usage == GraphicsTextureUsage::ShaderWrite)
    {
        createInfo.usage = VK_IMAGE_USAGE_SAMPLED_BIT | VK_IMAGE_USAGE_TRANSFER_DST_BIT | VK_IMAGE_USAGE_STORAGE_BIT;
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

VkFramebuffer CreateFramebuffer(VkDevice device, VkRenderPass renderPass, VkImageView* imageViews, uint32_t attachmentCount, uint32_t width, uint32_t height)
{
	VkFramebufferCreateInfo createInfo = { VK_STRUCTURE_TYPE_FRAMEBUFFER_CREATE_INFO };
	createInfo.renderPass = renderPass;
	createInfo.attachmentCount = attachmentCount;
	createInfo.pAttachments = imageViews;
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
	// TODO: Rewrite this to handle all cases!

	uint32_t attachmentCount = 1;

	// TODO: Handle the proper amount of attachments
    VkAttachmentDescription attachments[2] = {};

	// TODO: Handle RT operations defined in the renderPassDescriptor
	attachments[0].format = VulkanConvertTextureFormat(renderPassDescriptor.RenderTarget1TextureFormat.Value);
	attachments[0].samples = VK_SAMPLE_COUNT_1_BIT;
	attachments[0].loadOp = renderPassDescriptor.RenderTarget1ClearColor.HasValue ? VK_ATTACHMENT_LOAD_OP_CLEAR : VK_ATTACHMENT_LOAD_OP_LOAD;
	attachments[0].storeOp = VK_ATTACHMENT_STORE_OP_STORE;
	attachments[0].stencilLoadOp = VK_ATTACHMENT_LOAD_OP_DONT_CARE;
	attachments[0].stencilStoreOp = VK_ATTACHMENT_STORE_OP_DONT_CARE;
	attachments[0].initialLayout = VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL;
	attachments[0].finalLayout = VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL;

	if (renderPassDescriptor.DepthTexturePointer.HasValue)
	{
		attachmentCount++;

		VulkanTexture* depthTexture = (VulkanTexture*)renderPassDescriptor.DepthTexturePointer.Value;
		attachments[1].format = depthTexture->Format;
		attachments[1].samples = VK_SAMPLE_COUNT_1_BIT;
		attachments[1].loadOp = (renderPassDescriptor.DepthBufferOperation == GraphicsDepthBufferOperation::ClearWrite) ? VK_ATTACHMENT_LOAD_OP_CLEAR : VK_ATTACHMENT_LOAD_OP_LOAD;
		attachments[1].storeOp = VK_ATTACHMENT_STORE_OP_STORE;
		attachments[1].stencilLoadOp = VK_ATTACHMENT_LOAD_OP_DONT_CARE;
		attachments[1].stencilStoreOp = VK_ATTACHMENT_STORE_OP_DONT_CARE;
		attachments[1].initialLayout = VK_IMAGE_LAYOUT_DEPTH_ATTACHMENT_OPTIMAL;
		attachments[1].finalLayout = VK_IMAGE_LAYOUT_DEPTH_ATTACHMENT_OPTIMAL;
	}

	VkAttachmentReference colorAttachments = { 0, VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL };
	VkAttachmentReference depthAttachments = { 1, VK_IMAGE_LAYOUT_DEPTH_STENCIL_ATTACHMENT_OPTIMAL };

	VkSubpassDescription subpass = {};
	subpass.pipelineBindPoint = VK_PIPELINE_BIND_POINT_GRAPHICS;
	subpass.colorAttachmentCount = 1;
	subpass.pColorAttachments = &colorAttachments;

	if (renderPassDescriptor.DepthTexturePointer.HasValue)
	{
		subpass.pDepthStencilAttachment = &depthAttachments;
	}

	VkRenderPassCreateInfo createInfo = { VK_STRUCTURE_TYPE_RENDER_PASS_CREATE_INFO };
	createInfo.attachmentCount = attachmentCount;
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
	descriptorBinding.stageFlags = VK_SHADER_STAGE_ALL;

	VkDescriptorSetLayoutCreateInfo descriptorSetCreateInfo = { VK_STRUCTURE_TYPE_DESCRIPTOR_SET_LAYOUT_CREATE_INFO };
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
		globalBufferLayout = CreateDescriptorSetLayout(device, VK_DESCRIPTOR_TYPE_STORAGE_BUFFER, 2500);
	}

	return globalBufferLayout;
}

VkDescriptorSetLayout globalTextureLayout = nullptr;

VkDescriptorSetLayout GetGlobalTextureLayout(VkDevice device)
{
	if (globalTextureLayout == nullptr)
	{
		globalTextureLayout = CreateDescriptorSetLayout(device, VK_DESCRIPTOR_TYPE_SAMPLED_IMAGE, 2500);
	}

	return globalTextureLayout;
}

VkDescriptorSetLayout globalUavBufferLayout = nullptr;

VkDescriptorSetLayout GetGlobalUavBufferLayout(VkDevice device)
{
	if (globalUavBufferLayout == nullptr)
	{
		globalUavBufferLayout = CreateDescriptorSetLayout(device, VK_DESCRIPTOR_TYPE_STORAGE_BUFFER, 2500);
	}

	return globalUavBufferLayout;
}

VkDescriptorSetLayout globalUavTextureLayout = nullptr;

VkDescriptorSetLayout GetGlobalUavTextureLayout(VkDevice device)
{
	if (globalUavTextureLayout == nullptr)
	{
		globalUavTextureLayout = CreateDescriptorSetLayout(device, VK_DESCRIPTOR_TYPE_STORAGE_IMAGE, 2500);
	}

	return globalUavTextureLayout;
}

VkDescriptorSetLayout globalSamplerLayout = nullptr;

VkDescriptorSetLayout GetGlobalSamplerLayout(VkDevice device)
{
	if (globalSamplerLayout == nullptr)
	{
		globalSamplerLayout = CreateDescriptorSetLayout(device, VK_DESCRIPTOR_TYPE_SAMPLER, 1);
	}

	return globalSamplerLayout;
}

VkPipelineLayout CreateGraphicsPipelineLayout(VkDevice device, uint32_t parameterCount, uint32_t* layoutCount, VkDescriptorSetLayout** outputSetLayouts)
{
	// TODO: To replace with dynamic shader discovery
	VkDescriptorSetLayout setLayouts[] =
	{
		GetGlobalBufferLayout(device),
		GetGlobalTextureLayout(device),
		GetGlobalUavBufferLayout(device),
		GetGlobalUavTextureLayout(device),
		GetGlobalSamplerLayout(device)
	};

	VkPipelineLayoutCreateInfo layoutCreateInfo = { VK_STRUCTURE_TYPE_PIPELINE_LAYOUT_CREATE_INFO };
	layoutCreateInfo.pSetLayouts = setLayouts;
	layoutCreateInfo.setLayoutCount = ARRAYSIZE(setLayouts);

	// TODO: 
	VkPushConstantRange push_constant;
	push_constant.offset = 0;
	push_constant.size = parameterCount * sizeof(uint32_t);
	push_constant.stageFlags = VK_SHADER_STAGE_ALL;

	layoutCreateInfo.pPushConstantRanges = &push_constant;
	layoutCreateInfo.pushConstantRangeCount = 1;

	VkPipelineLayout layout = 0;
	AssertIfFailed(vkCreatePipelineLayout(device, &layoutCreateInfo, 0, &layout));

	*outputSetLayouts = setLayouts;
	*layoutCount = ARRAYSIZE(setLayouts);

	return layout;
}

VkIndirectCommandsLayoutNV CreateIndirectPipelineLayout(VkDevice device, bool isComputeShader, uint32_t parameterCount)
{
	// TODO: Skip compute shaders for now
	if (isComputeShader)
	{
		return nullptr;
	}

	VkIndirectCommandsLayoutTokenNV arguments[2] = {};
	arguments[0] = { VK_STRUCTURE_TYPE_INDIRECT_COMMANDS_LAYOUT_TOKEN_NV };
	arguments[0].tokenType = VK_INDIRECT_COMMANDS_TOKEN_TYPE_PUSH_CONSTANT_NV;
	arguments[0].stream = 0;
	arguments[0].pushconstantSize = parameterCount * sizeof(uint32_t);
	arguments[1] = { VK_STRUCTURE_TYPE_INDIRECT_COMMANDS_LAYOUT_TOKEN_NV };
	arguments[1].tokenType = VK_INDIRECT_COMMANDS_TOKEN_TYPE_DRAW_TASKS_NV;
	arguments[1].stream = 0;
	arguments[1].offset = parameterCount * sizeof(uint32_t);

	uint32_t strides[1] = {(3 + parameterCount) * sizeof(uint32_t)};

	VkIndirectCommandsLayoutCreateInfoNV createInfo = { VK_STRUCTURE_TYPE_INDIRECT_COMMANDS_LAYOUT_CREATE_INFO_NV };
    createInfo.flags = VK_INDIRECT_COMMANDS_LAYOUT_USAGE_UNORDERED_SEQUENCES_BIT_NV;
    createInfo.pipelineBindPoint = isComputeShader ? VK_PIPELINE_BIND_POINT_COMPUTE : VK_PIPELINE_BIND_POINT_GRAPHICS;
    createInfo.tokenCount = ARRAYSIZE(arguments);
    createInfo.pTokens = arguments;
	createInfo.streamCount = 1;
	createInfo.pStreamStrides = strides;

	VkIndirectCommandsLayoutNV layout = nullptr;
	AssertIfFailed(vkCreateIndirectCommandsLayoutNV(device, &createInfo, nullptr, &layout));

	return layout;
}

VkPipelineColorBlendAttachmentState VulkanInitBlendState(GraphicsBlendOperation blendOperation)
{
	switch (blendOperation)
	{
		case GraphicsBlendOperation::AlphaBlending:
			return {
				true,
				VK_BLEND_FACTOR_SRC_ALPHA, VK_BLEND_FACTOR_ONE_MINUS_SRC_ALPHA, VK_BLEND_OP_ADD,
				VK_BLEND_FACTOR_SRC_ALPHA, VK_BLEND_FACTOR_ONE_MINUS_SRC_ALPHA, VK_BLEND_OP_ADD,
				VK_COLOR_COMPONENT_R_BIT | VK_COLOR_COMPONENT_G_BIT | VK_COLOR_COMPONENT_B_BIT | VK_COLOR_COMPONENT_A_BIT
			};

		default:
			return {
				false,
				VK_BLEND_FACTOR_ONE, VK_BLEND_FACTOR_ZERO, VK_BLEND_OP_ADD,
				VK_BLEND_FACTOR_ONE, VK_BLEND_FACTOR_ZERO, VK_BLEND_OP_ADD,
				VK_COLOR_COMPONENT_R_BIT | VK_COLOR_COMPONENT_G_BIT | VK_COLOR_COMPONENT_B_BIT | VK_COLOR_COMPONENT_A_BIT
			};
	}
}

VkPipeline CreateComputePipeline(VkDevice device, VkPipelineLayout layout, VulkanShader* shader)
{
	VkComputePipelineCreateInfo createInfo = { VK_STRUCTURE_TYPE_COMPUTE_PIPELINE_CREATE_INFO };

	VkPipelineShaderStageCreateInfo stage = { VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO };
	stage.stage = VK_SHADER_STAGE_COMPUTE_BIT;
	stage.module = shader->ComputeShaderMethod;
	stage.pName = "ComputeMain";

	createInfo.stage = stage;
	createInfo.layout = layout;

	//TODO: Use the pipelinecache !
	VkPipeline pipeline = 0;
	AssertIfFailed(vkCreateComputePipelines(device, nullptr, 1, &createInfo, 0, &pipeline));

	return pipeline;
}

VkPipeline CreateGraphicsPipeline(VkDevice device, VkRenderPass renderPass, VkPipelineLayout layout, GraphicsRenderPassDescriptor renderPassDescriptor, VulkanShader* shader)
{
	VkGraphicsPipelineCreateInfo createInfo = { VK_STRUCTURE_TYPE_GRAPHICS_PIPELINE_CREATE_INFO };

	uint32_t stagesCount = 2;

	VkPipelineShaderStageCreateInfo stages[3] = {};
	stages[0].sType = VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO;
	stages[0].stage = VK_SHADER_STAGE_MESH_BIT_NV;
	stages[0].module = shader->MeshShaderMethod;
	stages[0].pName = "MeshMain";
	stages[1].sType = VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO;
	stages[1].stage = VK_SHADER_STAGE_FRAGMENT_BIT;
	stages[1].module = shader->PixelShaderMethod;
	stages[1].pName = "PixelMain";

	if (shader->AmplificationShaderMethod)
	{
		stages[2].sType = VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO;
		stages[2].stage = VK_SHADER_STAGE_TASK_BIT_NV;
		stages[2].module = shader->AmplificationShaderMethod;
		stages[2].pName = "AmplificationMain";

		stagesCount++;
	}

	createInfo.stageCount = stagesCount;
	createInfo.pStages = stages;

	VkPipelineInputAssemblyStateCreateInfo inputAssemblyState = { VK_STRUCTURE_TYPE_PIPELINE_INPUT_ASSEMBLY_STATE_CREATE_INFO };
	inputAssemblyState.topology = renderPassDescriptor.PrimitiveType == GraphicsPrimitiveType::Triangle ? VK_PRIMITIVE_TOPOLOGY_TRIANGLE_LIST : VK_PRIMITIVE_TOPOLOGY_LINE_LIST;
	createInfo.pInputAssemblyState = &inputAssemblyState;

	VkPipelineViewportStateCreateInfo viewportState = { VK_STRUCTURE_TYPE_PIPELINE_VIEWPORT_STATE_CREATE_INFO };
	viewportState.viewportCount = 1;
	viewportState.scissorCount = 1;
	createInfo.pViewportState = &viewportState;

	VkPipelineRasterizationStateCreateInfo rasterizationState = { VK_STRUCTURE_TYPE_PIPELINE_RASTERIZATION_STATE_CREATE_INFO };
	rasterizationState.lineWidth = 1.0f;
	rasterizationState.cullMode = VK_CULL_MODE_BACK_BIT;
	rasterizationState.frontFace = VK_FRONT_FACE_CLOCKWISE;
	createInfo.pRasterizationState = &rasterizationState;

	VkPipelineMultisampleStateCreateInfo multisampleState = { VK_STRUCTURE_TYPE_PIPELINE_MULTISAMPLE_STATE_CREATE_INFO };
	multisampleState.rasterizationSamples = VK_SAMPLE_COUNT_1_BIT;
	createInfo.pMultisampleState = &multisampleState;

	VkPipelineDepthStencilStateCreateInfo depthStencilState = { VK_STRUCTURE_TYPE_PIPELINE_DEPTH_STENCIL_STATE_CREATE_INFO };

	if (renderPassDescriptor.DepthBufferOperation != GraphicsDepthBufferOperation::DepthNone)
	{
		depthStencilState.depthTestEnable = true;

		if (renderPassDescriptor.DepthBufferOperation == GraphicsDepthBufferOperation::ClearWrite ||
			renderPassDescriptor.DepthBufferOperation == GraphicsDepthBufferOperation::Write)
		{
			depthStencilState.depthWriteEnable = true;
		}

		if (renderPassDescriptor.DepthBufferOperation == GraphicsDepthBufferOperation::CompareEqual)
		{
			depthStencilState.depthCompareOp = VK_COMPARE_OP_EQUAL;
		}

		else
		{
			depthStencilState.depthCompareOp = VK_COMPARE_OP_GREATER;
		}
	}

	createInfo.pDepthStencilState = &depthStencilState;

	VkPipelineColorBlendAttachmentState colorAttachmentState = VulkanInitBlendState(GraphicsBlendOperation::None);
	
	if (renderPassDescriptor.RenderTarget1BlendOperation.HasValue)
	{
		colorAttachmentState = VulkanInitBlendState(renderPassDescriptor.RenderTarget1BlendOperation.Value);
	}

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

VkBufferMemoryBarrier CreateBufferTransitionBarrier(VkBuffer buffer, uint32_t sizeInBytes, VkAccessFlags oldAccess, VkAccessFlags newAccess)
{
	VkBufferMemoryBarrier result = { VK_STRUCTURE_TYPE_BUFFER_MEMORY_BARRIER };

	result.srcAccessMask = oldAccess;
	result.dstAccessMask = newAccess;
	result.srcQueueFamilyIndex = VK_QUEUE_FAMILY_IGNORED;
	result.dstQueueFamilyIndex = VK_QUEUE_FAMILY_IGNORED;
	result.buffer = buffer;
	result.offset = 0;
    result.size = sizeInBytes;

	return result;
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

void TransitionBufferToState(VulkanCommandList* commandList, VulkanGraphicsBuffer* buffer, VkAccessFlags destinationAccess, bool isTransfer = false)
{
	if (buffer->ResourceAccess != destinationAccess)
	{
		auto barrier = CreateBufferTransitionBarrier(buffer->BufferObject, buffer->SizeInBytes, buffer->ResourceAccess, destinationAccess);
		
		if (!isTransfer)
		{
			vkCmdPipelineBarrier(commandList->CommandBufferObject, VK_PIPELINE_STAGE_ALL_GRAPHICS_BIT, VK_PIPELINE_STAGE_ALL_GRAPHICS_BIT, VK_DEPENDENCY_BY_REGION_BIT, 0, 0, 1, &barrier, 0, 0);
		}

		else
		{
			vkCmdPipelineBarrier(commandList->CommandBufferObject, VK_PIPELINE_STAGE_TRANSFER_BIT, VK_PIPELINE_STAGE_TRANSFER_BIT, VK_DEPENDENCY_BY_REGION_BIT, 0, 0, 1, &barrier, 0, 0);
		}

		buffer->ResourceAccess = destinationAccess;
	}
}

void TransitionTextureToState(VulkanCommandList* commandList, VulkanTexture* texture, VkImageLayout destinationState, bool isTransfer = false)
{
	// TODO: Handle texture accesses, currently we only handle the image layout

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