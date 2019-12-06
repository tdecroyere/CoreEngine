import Cocoa
import QuartzCore.CAMetalLayer

class MacOSMetalView: NSView, MetalView {
    @available(*, unavailable) public required init?(coder: NSCoder) { fatalError() }

    public var metalLayer: CAMetalLayer
    
    public override init(frame: CGRect) {
        self.metalLayer = CAMetalLayer()
        super.init(frame: frame)
        self.wantsLayer = true
        self.layerContentsRedrawPolicy = .onSetNeedsDisplay
    }

    override var wantsUpdateLayer: Bool {
        return true
    }
    
    public override func makeBackingLayer() -> CALayer {
        return metalLayer
    }

    public override func updateLayer() {
    }
}