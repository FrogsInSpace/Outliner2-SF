struct layerTools
(
  --Returns the parent layer for the supplied layer argument,
  --returns undefined for a rootlayer.
  function getParent layer =
  (
    local parent = layer.getParent();

    if( parent != undefined) then
    (
     -- format "LayerTools->GetParent %=%\n" (classof(parentLayer)) (if(parent != undefined) then parent.Name else "<undefined>");
      parent.layerAsRefTarg;
    )
    else undefined
  ),

  --Sets the parent layer for the supplied layer argument.
  --Use undefined to unlink the layer.

  mapped function setParent layer parent =
  (
    if( layer != undefined ) do
    (
      -- format "LayerTools->SetParent % -> %\n" layer (if(parent != undefined) then parent.name else "<root>")

      if( isKindOf layer Base_Layer ) do
        layer = LayerManager.getLayerFromName layer.name;

      if( isKindOf parent Base_Layer ) do
        parent = LayerManager.getLayerFromName parent.name;

      layer.SetParent parent;

      --Update tree.
      if (outliner_status != undefined AND outliner_status.windowOpen) do
      (
        local pHandle = if (parent == undefined) then outliner.tree.Scene.RootHandle else (getHandleByAnim parent.layerAsRefTarg);
        
        outliner.tree.SetLayerParent (getHandleByAnim layer.layerAsRefTarg) pHandle;
      )

      ok;
    )
  ),

  --Returns an array with layers that are children of the supplied layer.
  --If recursive is not supplied or false, then only layers directly beneath
  --the supplied layer are included. Otherwise it includes the entire layer
  --hierarchy downwards.
  function getChildren layer recursive:false =
  (
    try
    (
      local children = #();
      
      if( layer != undefined ) do
      (
        for i = 1 to layer.getNumChildren() do
        (
          local childLayer = (layer.GetChild i).layerAsRefTarg
          
          append children childLayer
          
          if (recursive) do
            join children (layerTools.getChildren childLayer recursive:recursive);
        )
      )
        --Return children array.
        children;
    )
    catch fileIn (outliner.dir + "crash_report.ms")
  ),

   --Returns the value of a property on the layer. It will return the layer's
  --own value, regardless of whether it has been overridden by a parent layer.
  function getLayerProperty layer prop =
  (
    try
    (
      local propValue;

      if (isProperty layer prop) then
        propValue = getProperty layer prop;

      propValue;
    )
    catch fileIn (outliner.dir + "crash_report.ms")
  ),


    --Sets a property on the layer and propagates it to its children.
    mapped function setLayerProperty layer prop value isSetByParent:false =
    (      
      try
      (
        if (isProperty layer prop) do
        (
          --local ca = outlinerNestedLayers.getCA();
          local parentLayer = layerTools.getParent layer;
          
          if (parentLayer == undefined) then
          (
            --Only set the property on the layer itself.
            setProperty layer prop value;
          )
          else
          (
            local parentValue = getProperty parentLayer prop;
            local ownValue;

            --Get / store own value.
            if (isSetByParent AND parentValue) then
              ownValue = layerTools.getLayerProperty layer prop;
            else
              ownValue = value;


            format "setLayerProperty: % '%=%, IsSetByParent:%,( p:%)\n" layer.name prop (parentValue OR ownValue) isSetByParent parentLayer       
            
            setProperty layer prop (parentValue OR ownValue);
          )

          --Propagate to children.
          layerTools.setLayerProperty (layerTools.getChildren layer) prop value isSetByParent:true;
        )

        ok;
      )
      catch fileIn (outliner.dir + "crash_report.ms")
    )
)