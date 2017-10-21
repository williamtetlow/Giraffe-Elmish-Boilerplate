class PluginBuilder {
  constructor() {
    this.buildingDevelopmentPlugins = false;
    this.buildingProductionPlugins = false;

    this.developmentPlugins = [];
    this.productionPlugins = [];
  }

  inDevelopment() {
    this.isDev(true);
    return this;
  }

  inProduction() {
    this.isDev(false);
    return this;
  }

  inBothEnvironments() {
    this.buildingDevelopmentPlugins = true;
    this.buildingProductionPlugins = true;

    return this;
  }

  use(plugin, ...opts) {
    if (this.buildingDevelopmentPlugins) {
      this.developmentPlugins.push(new plugin(...opts));
    }
  
    if (this.buildingProductionPlugins) {
      this.productionPlugins.push(new plugin(...opts));
    }
    return this;
  }

  build(isProduction) {
    return isProduction
      ? this.productionPlugins
      : this.developmentPlugins;
  }

  isDev(isDev) {
    this.buildingDevelopmentPlugins = isDev;
    this.buildingProductionPlugins = !isDev;
  }
}

module.exports = Object.freeze({
  plugins: () => {
    return new PluginBuilder();
  },
});
